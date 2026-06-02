using System.Collections.Generic;
using System.Linq;

namespace ElectricalSim.Practice.Netlist
{
    public sealed class ComponentMappingResult
    {
        public Dictionary<string, string> StandardToStudent { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> StudentToStandard { get; } = new Dictionary<string, string>();
        public List<string> MissingComponentMessages { get; } = new List<string>();
        public List<string> ExtraComponentMessages { get; } = new List<string>();
        public bool Ambiguous { get; set; }
    }

    public static class ComponentMappingSolver
    {
        private const int MaxPermutationCount = 720;

        public static ComponentMappingResult Solve(PracticeNetlist standard, PracticeNetlist student)
        {
            var result = new ComponentMappingResult();
            if (standard == null || student == null)
            {
                return result;
            }

            var standardGroups = standard.Components.Values.GroupBy(c => c.DefinitionName).ToDictionary(g => g.Key, g => g.ToList());
            var studentGroups = student.Components.Values.GroupBy(c => c.DefinitionName).ToDictionary(g => g.Key, g => g.ToList());
            var allDefinitionNames = new HashSet<string>(standardGroups.Keys);
            allDefinitionNames.UnionWith(studentGroups.Keys);

            var candidates = new List<Dictionary<string, string>> { new Dictionary<string, string>() };

            foreach (var definitionName in allDefinitionNames.OrderBy(n => n))
            {
                standardGroups.TryGetValue(definitionName, out var standardComponents);
                studentGroups.TryGetValue(definitionName, out var studentComponents);
                standardComponents = standardComponents ?? new List<PracticeNetlistComponent>();
                studentComponents = studentComponents ?? new List<PracticeNetlistComponent>();

                if (studentComponents.Count < standardComponents.Count)
                {
                    result.MissingComponentMessages.Add("\u7f3a\u5c11 " + (standardComponents.Count - studentComponents.Count) + " \u4e2a " + GetDefinitionLabel(standardComponents, studentComponents, definitionName) + "\u3002");
                }
                else if (studentComponents.Count > standardComponents.Count)
                {
                    result.ExtraComponentMessages.Add("\u591a\u4f59 " + (studentComponents.Count - standardComponents.Count) + " \u4e2a " + GetDefinitionLabel(standardComponents, studentComponents, definitionName) + "\u3002");
                }

                if (standardComponents.Count == 0 || studentComponents.Count == 0)
                {
                    continue;
                }

                var mappingOptions = BuildMappingOptions(standardComponents, studentComponents);
                var nextCandidates = new List<Dictionary<string, string>>();
                foreach (var candidate in candidates)
                {
                    foreach (var option in mappingOptions)
                    {
                        var copy = new Dictionary<string, string>(candidate);
                        foreach (var kvp in option)
                        {
                            copy[kvp.Key] = kvp.Value;
                        }

                        nextCandidates.Add(copy);
                    }
                }

                candidates = nextCandidates.Count > 0 ? nextCandidates : candidates;
            }

            var bestScore = int.MinValue;
            var bestCandidates = new List<Dictionary<string, string>>();
            foreach (var candidate in candidates)
            {
                var score = ScoreMapping(candidate, standard, student);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidates.Clear();
                    bestCandidates.Add(candidate);
                }
                else if (score == bestScore)
                {
                    bestCandidates.Add(candidate);
                }
            }

            var best = bestCandidates.Count > 0 ? bestCandidates[0] : new Dictionary<string, string>();
            foreach (var kvp in best)
            {
                result.StandardToStudent[kvp.Key] = kvp.Value;
                result.StudentToStandard[kvp.Value] = kvp.Key;
            }

            var hasReliableMapping = result.StandardToStudent.Count == standard.Components.Count && bestScore >= 0;
            result.Ambiguous = bestCandidates.Count > 1 && !hasReliableMapping;
            return result;
        }

        private static List<Dictionary<string, string>> BuildMappingOptions(List<PracticeNetlistComponent> standardComponents, List<PracticeNetlistComponent> studentComponents)
        {
            var standardIds = standardComponents.Select(c => c.ComponentId).ToList();
            var studentIds = studentComponents.Select(c => c.ComponentId).ToList();
            var length = System.Math.Min(standardIds.Count, studentIds.Count);
            var permutations = GetPermutations(studentIds, length);
            if (permutations.Count > MaxPermutationCount)
            {
                permutations = new List<List<string>> { studentIds.Take(length).ToList() };
            }

            var options = new List<Dictionary<string, string>>();
            foreach (var permutation in permutations)
            {
                var option = new Dictionary<string, string>();
                for (var i = 0; i < length; i++)
                {
                    option[standardIds[i]] = permutation[i];
                }

                options.Add(option);
            }

            return options;
        }

        private static int ScoreMapping(Dictionary<string, string> standardToStudent, PracticeNetlist standard, PracticeNetlist student)
        {
            var score = standardToStudent.Count * 1000;
            foreach (var connection in standard.DirectConnections)
            {
                if (!TryMapTerminal(connection.StartKey, standardToStudent, out var mappedStart) ||
                    !TryMapTerminal(connection.EndKey, standardToStudent, out var mappedEnd))
                {
                    score -= 100;
                    continue;
                }

                score += student.AreConnected(mappedStart, mappedEnd) ? 20 : -50;
            }

            var studentToStandard = standardToStudent.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            foreach (var connection in student.DirectConnections)
            {
                if (!TryMapTerminal(connection.StartKey, studentToStandard, out var mappedStart) ||
                    !TryMapTerminal(connection.EndKey, studentToStandard, out var mappedEnd))
                {
                    score -= 20;
                    continue;
                }

                score += standard.AreConnected(mappedStart, mappedEnd) ? 8 : -40;
            }

            return score;
        }

        public static bool TryMapTerminal(string sourceTerminalKey, Dictionary<string, string> componentMap, out string mappedTerminalKey)
        {
            mappedTerminalKey = null;
            SplitTerminalKey(sourceTerminalKey, out var componentId, out var terminalId);
            if (string.IsNullOrWhiteSpace(componentId) || !componentMap.TryGetValue(componentId, out var mappedComponentId))
            {
                return false;
            }

            mappedTerminalKey = PracticeNetlistTerminal.MakeKey(mappedComponentId, terminalId);
            return true;
        }

        public static void SplitTerminalKey(string terminalKey, out string componentId, out string terminalId)
        {
            componentId = string.Empty;
            terminalId = string.Empty;
            if (string.IsNullOrWhiteSpace(terminalKey))
            {
                return;
            }

            var index = terminalKey.IndexOf('.');
            if (index < 0)
            {
                componentId = terminalKey;
                return;
            }

            componentId = terminalKey.Substring(0, index);
            terminalId = terminalKey.Substring(index + 1);
        }

        private static List<List<string>> GetPermutations(List<string> source, int length)
        {
            if (length <= 0)
            {
                return new List<List<string>> { new List<string>() };
            }

            var result = new List<List<string>>();
            BuildPermutations(source, length, new List<string>(), result);
            return result;
        }

        private static void BuildPermutations(List<string> source, int length, List<string> current, List<List<string>> result)
        {
            if (current.Count == length)
            {
                result.Add(new List<string>(current));
                return;
            }

            foreach (var item in source)
            {
                if (current.Contains(item))
                {
                    continue;
                }

                current.Add(item);
                BuildPermutations(source, length, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }

        private static string GetDefinitionLabel(List<PracticeNetlistComponent> standardComponents, List<PracticeNetlistComponent> studentComponents, string fallback)
        {
            var component = studentComponents.FirstOrDefault() ?? standardComponents.FirstOrDefault();
            return component != null ? component.DisplayName : fallback;
        }
    }
}

