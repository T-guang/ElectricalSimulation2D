using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ElectricalSim.AI
{
    public sealed class RealAIAssistantService : IAIAssistantService
    {
        private const string SystemPrompt = "\u4f60\u662f\u7535\u5de5\u6570\u5b57\u5b66\u751f\u4eff\u771f\u7cfb\u7edf\u4e2d\u7684 AI \u52a9\u6559\u3002\u4f60\u53ea\u80fd\u89e3\u91ca\u7535\u8def\u3001\u68c0\u67e5\u63a5\u7ebf\u601d\u8def\u548c\u8bb2\u89e3\u7535\u5de5\u77e5\u8bc6\uff0c\u4e0d\u80fd\u8981\u6c42\u7528\u6237\u8fdb\u884c\u5371\u9669\u771f\u5b9e\u5e26\u7535\u64cd\u4f5c\uff0c\u4e0d\u80fd\u76f4\u63a5\u4fee\u6539\u7535\u8def\u3002\u56de\u7b54\u8981\u7b80\u6d01\u3001\u6e05\u695a\u3001\u9762\u5411\u5b66\u751f\u3002";
        private const string MissingConfigMessage = "\u771f\u5b9e AI API \u5c1a\u672a\u914d\u7f6e\u3002";
        private const string RequestFailureMessage = "\u771f\u5b9e AI \u8bf7\u6c42\u5931\u8d25\uff0c\u8bf7\u68c0\u67e5\u7f51\u7edc\u3001API Key \u6216 Endpoint\u3002";

        private readonly AIAssistantConfig config;
        private readonly MonoBehaviour coroutineRunner;

        public bool IsConfigured => config != null && config.IsConfigured;

        public RealAIAssistantService(AIAssistantConfig config, MonoBehaviour coroutineRunner)
        {
            this.config = config;
            this.coroutineRunner = coroutineRunner;
        }

        public void Ask(string userQuestion, string circuitSummary, Action<string> onSuccess, Action<string> onError)
        {
            if (!IsConfigured)
            {
                onError?.Invoke(MissingConfigMessage);
                return;
            }

            if (coroutineRunner == null)
            {
                onError?.Invoke(RequestFailureMessage);
                return;
            }

            coroutineRunner.StartCoroutine(SendChatCompletion(userQuestion, circuitSummary, onSuccess, onError));
        }

        private IEnumerator SendChatCompletion(string userQuestion, string circuitSummary, Action<string> onSuccess, Action<string> onError)
        {
            var requestBody = new ChatCompletionRequest
            {
                model = config.Model,
                messages = new[]
                {
                    new ChatMessage { role = "system", content = SystemPrompt },
                    new ChatMessage { role = "user", content = BuildUserContent(userQuestion, circuitSummary) }
                },
                temperature = 0.3f,
                max_tokens = 800
            };

            var json = JsonUtility.ToJson(requestBody);
            using (var request = new UnityWebRequest(config.Endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                var body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 60;
                request.SetRequestHeader("Authorization", "Bearer " + config.ApiKey);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (IsRequestFailed(request))
                {
                    var errorBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    Debug.LogWarning($"Real AI request failed: {request.responseCode} {request.error}. Body: {errorBody}");
                    onError?.Invoke(RequestFailureMessage);
                    yield break;
                }

                var responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                var content = ParseAssistantContent(responseText);
                if (string.IsNullOrWhiteSpace(content))
                {
                    Debug.LogWarning("Real AI response did not contain choices[0].message.content.");
                    onError?.Invoke(RequestFailureMessage);
                    yield break;
                }

                onSuccess?.Invoke(content.Trim());
            }
        }

        private static bool IsRequestFailed(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.ProtocolError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

        private static string BuildUserContent(string userQuestion, string circuitSummary)
        {
            return "\u7528\u6237\u95ee\u9898\uff1a" + (userQuestion ?? string.Empty) + "\n\n\u5f53\u524d\u7535\u8def\u6458\u8981\uff1a" + (circuitSummary ?? string.Empty);
        }

        private static string ParseAssistantContent(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return string.Empty;
            }

            try
            {
                var response = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
                if (response != null && response.choices != null && response.choices.Length > 0)
                {
                    var message = response.choices[0].message;
                    if (message != null)
                    {
                        return message.content;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to parse real AI response: " + ex.Message);
            }

            return string.Empty;
        }

        [Serializable]
        private sealed class ChatCompletionRequest
        {
            public string model;
            public ChatMessage[] messages;
            public float temperature;
            public int max_tokens;
        }

        [Serializable]
        private sealed class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private sealed class ChatCompletionResponse
        {
            public ChatChoice[] choices = Array.Empty<ChatChoice>();
        }

        [Serializable]
        private sealed class ChatChoice
        {
            public ChatMessage message = new ChatMessage();
        }
    }
}
