using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI.CommonTools
{
    public sealed class ElectricianCalculatorController : MonoBehaviour
    {
        private RectTransform tabsArea;
        private RectTransform contentArea;
        private Button[] tabs;
        private RectTransform[] pages;
        private int currentTab = 0;

        // Load Current
        private Dropdown loadSupplyType;
        private Dropdown loadType;
        private InputField loadPower;
        private Dropdown loadPowerUnit;
        private InputField loadCosPhi;
        private InputField loadEta;
        private InputField loadCount;
        private Text loadResult;

        // Wire and Breaker
        private InputField wireCurrent;
        private Dropdown wireMaterial;
        private Dropdown wireCondition;
        private Dropdown wireCircuitType;
        private Text wireResult;

        // Motor Startup
        private InputField motorPower;
        private InputField motorVoltage;
        private InputField motorCosPhi;
        private InputField motorEta;
        private Dropdown motorStartupType;
        private Text motorResult;

        // Multi Load
        private Dropdown multiSupplyType;
        private InputField multiLightCount;
        private InputField multiLightPower;
        private InputField multiFanCount;
        private InputField multiFanPower;
        private InputField multiMotorCount;
        private InputField multiMotorPower;
        private InputField multiOtherPower;
        private InputField multiCosPhi;
        private Text multiResult;

        private static readonly Color PrimaryBlue = new Color(0.15f, 0.39f, 0.92f);
        private static readonly Color TextDark = new Color(0.07f, 0.11f, 0.18f);
        private static readonly Color TextMuted = new Color(0.35f, 0.42f, 0.52f);
        private static readonly Color CardBg = Color.white;
        private static readonly Color ErrorColor = new Color(0.89f, 0.26f, 0.20f);

        private void Awake()
        {
            BuildUI();
            SelectTab(0);
        }

        private void BuildUI()
        {
            ClearChildren(transform);
            
            var header = CreateText("Header", transform, "回路参数估算工具", 24, FontStyle.Bold, TextDark);
            SetRect(header.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -22), new Vector2(280, 36));

            var subHeader = CreateText("SubHeader", transform, "用于估算负载电流、线径载流量、空开匹配和电机启动电流。结果仅用于教学参考，不作为真实工程设计依据。", 14, FontStyle.Normal, TextMuted);
            SetRect(subHeader.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(-56, 32));

            tabsArea = CreateRect("TabsArea", transform);
            SetRect(tabsArea, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -104), new Vector2(-56, 44));
            var hLayout = tabsArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            contentArea = CreateRect("ContentArea", transform);
            SetRect(contentArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(-56, -164));

            tabs = new Button[4];
            pages = new RectTransform[4];

            string[] tabNames = { "负载电流估算", "线径与空开估算", "电机启动估算", "多负载回路估算" };
            for (int i = 0; i < 4; i++)
            {
                var captured = i;
                tabs[i] = CreateTabButton(tabNames[i], () => SelectTab(captured));
                pages[i] = BuildPageRoot("Page_" + i);
            }

            BuildLoadCurrentPage(pages[0]);
            BuildWireBreakerPage(pages[1]);
            BuildMotorStartupPage(pages[2]);
            BuildMultiLoadPage(pages[3]);
        }

        private void SelectTab(int index)
        {
            currentTab = index;
            for (int i = 0; i < 4; i++)
            {
                var img = tabs[i].GetComponent<Image>();
                var txt = tabs[i].GetComponentInChildren<Text>();
                bool active = (i == index);
                img.color = active ? PrimaryBlue : new Color(0.94f, 0.97f, 1f);
                txt.color = active ? Color.white : TextDark;
                txt.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                pages[i].gameObject.SetActive(active);
            }
        }

        private Button CreateTabButton(string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(tabsArea, false);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(action);
            var txt = CreateText("Text", go.transform, label, 16, FontStyle.Normal, TextDark);
            txt.alignment = TextAnchor.MiddleCenter;
            SetRect(txt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return btn;
        }

        private RectTransform BuildPageRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(contentArea, false);
            SetRect(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            go.GetComponent<Image>().color = CardBg;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.05f);
            outline.effectDistance = new Vector2(1, -1);
            return go.GetComponent<RectTransform>();
        }

        private void BuildLoadCurrentPage(RectTransform page)
        {
            var container = CreateScrollableContent(page);
            CreateSectionTitle(container, "输入参数估算负载电流");
            
            loadSupplyType = CreateDropdown(container, "供电类型", new[] { "单相 220V", "三相 380V" });
            loadType = CreateDropdown(container, "负载类型", new[] { "照明灯", "电风扇", "电热负载", "三相电机", "其他负载" });
            
            var powerRow = CreateRow(container);
            loadPower = CreateInputFieldRaw(powerRow, "额定功率");
            loadPowerUnit = CreateDropdownRaw(powerRow, "单位", new[] { "W", "kW" });
            
            loadCosPhi = CreateInputField(container, "功率因数 cosφ", "纯电阻默认1.0，感性约0.8");
            loadCosPhi.text = "1";
            loadEta = CreateInputField(container, "效率 η", "电机默认0.85，非电机默认1");
            loadEta.text = "1";
            loadCount = CreateInputField(container, "负载数量");
            loadCount.text = "1";
            
            loadType.onValueChanged.AddListener(val => {
                if(val == 0 || val == 2) { loadCosPhi.text = "1"; loadEta.text = "1"; } // 照明/电热
                else if(val == 1) { loadCosPhi.text = "0.8"; loadEta.text = "1"; } // 风扇
                else if(val == 3) { loadCosPhi.text = "0.8"; loadEta.text = "0.85"; loadSupplyType.value = 1; } // 电机
            });

            CreateCalcButton(container, CalculateLoadCurrent);
            loadResult = CreateResultArea(container);
        }

        private void CalculateLoadCurrent()
        {
            try
            {
                double pRaw = double.Parse(loadPower.text);
                double pW = loadPowerUnit.value == 1 ? pRaw * 1000 : pRaw;
                double cosPhi = double.Parse(loadCosPhi.text);
                double eta = double.Parse(loadEta.text);
                double count = double.Parse(loadCount.text);
                if (pW <= 0 || cosPhi <= 0 || eta <= 0 || count <= 0) throw new Exception("参数必须大于 0");

                double singleI = 0;
                string formula = "";
                string process = "";
                
                if (loadSupplyType.value == 0) // 220V
                {
                    singleI = pW / (220 * cosPhi * eta);
                    formula = "I = P / (U × cosφ × η)";
                    process = $"I = {pW:0.##} / (220 × {cosPhi:0.##} × {eta:0.##})";
                }
                else // 380V
                {
                    singleI = pW / (1.7320508 * 380 * cosPhi * eta);
                    formula = "I = P / (√3 × U × cosφ × η)";
                    process = $"I = {pW:0.##} / (1.732 × 380 × {cosPhi:0.##} × {eta:0.##})";
                }

                double totalI = singleI * count;

                string msg = $"估算结果：\n单个电流 I ≈ {singleI:0.###} A\n总电流 I_total ≈ {totalI:0.###} A\n\n";
                msg += $"使用公式：\n{formula}\n";
                msg += $"代入过程：\n{process}\n\n";
                msg += "教学说明：\n该结果为教学估算值。单相公式 U=220V，三相公式 U=380V。实际工程应用中还需考虑启动电流、线路损耗和设备真实的铭牌参数。";
                ShowSuccess(loadResult, msg);
            }
            catch (Exception ex)
            {
                ShowError(loadResult, "参数填写错误。" + ex.Message);
            }
        }

        private void BuildWireBreakerPage(RectTransform page)
        {
            var container = CreateScrollableContent(page);
            CreateSectionTitle(container, "输入负载电流估算线径与空开");
            
            wireCurrent = CreateInputField(container, "负载总电流 (A)");
            wireMaterial = CreateDropdown(container, "导线材料", new[] { "铜", "铝" });
            wireCondition = CreateDropdown(container, "敷设条件", new[] { "普通/明敷", "穿管敷设", "高温环境", "多根并行" });
            wireCircuitType = CreateDropdown(container, "回路类型", new[] { "普通照明/插座", "电机回路" });
            
            CreateCalcButton(container, CalculateWireBreaker);
            wireResult = CreateResultArea(container);
        }

        private void CalculateWireBreaker()
        {
            try
            {
                double i = double.Parse(wireCurrent.text);
                if (i <= 0) throw new Exception("电流必须大于 0");

                bool isCopper = wireMaterial.value == 0;
                double minDensity = isCopper ? 5 : 3;
                double maxDensity = isCopper ? 8 : 5;
                
                // Adjust based on condition
                string condTip = "";
                if (wireCondition.value == 1) { maxDensity -= 1; condTip = "穿管散热差，采用保守电流密度。"; }
                if (wireCondition.value == 2) { maxDensity -= 1; minDensity -= 0.5; condTip = "高温降额，需增加截面积。"; }
                if (wireCondition.value == 3) { maxDensity -= 1; minDensity -= 0.5; condTip = "多根并行互相加热，需降额。"; }

                double minArea = i / maxDensity;
                double maxArea = i / minDensity;
                
                string mat = isCopper ? "铜" : "铝";
                
                double breakerMultiplier = wireCircuitType.value == 1 ? 1.5 : 1.2;
                double recommendedBreaker = i * breakerMultiplier;

                string msg = $"估算结果：\n";
                msg += $"推荐线径范围：{minArea:0.##} mm² ～ {maxArea:0.##} mm² ({mat}线)\n";
                msg += $"常见可选线径提示：1.5, 2.5, 4, 6, 10, 16 mm²（请向上取整）。\n";
                msg += $"空开建议范围：约 {recommendedBreaker:0.#} A\n\n";
                
                msg += $"代入过程：\n截面积 S = 电流 I / 安全载流密度\n按 {mat}线 {minDensity:0.#}~{maxDensity:0.#} A/mm² 估算：S = {i} / {maxDensity:0.#} ~ {minDensity:0.#}\n\n";
                msg += $"教学说明：\n{condTip}\n如果为电机回路，空开额定电流需适当放大以躲过启动电流。\n\n";
                msg += "注意事项：\n该估算仅供参考，不代替真实工程规范选型。工程中必须查阅《电工手册》并保证空开动作电流小于导线极限载流量。";
                
                ShowSuccess(wireResult, msg);
            }
            catch (Exception ex)
            {
                ShowError(wireResult, "参数填写错误。" + ex.Message);
            }
        }

        private void BuildMotorStartupPage(RectTransform page)
        {
            var container = CreateScrollableContent(page);
            CreateSectionTitle(container, "三相电机启动电流估算");
            
            motorPower = CreateInputField(container, "电机功率 (kW)");
            motorVoltage = CreateInputField(container, "线电压 U (V)");
            motorVoltage.text = "380";
            motorCosPhi = CreateInputField(container, "功率因数 cosφ");
            motorCosPhi.text = "0.8";
            motorEta = CreateInputField(container, "效率 η");
            motorEta.text = "0.85";
            motorStartupType = CreateDropdown(container, "启动方式", new[] { "直接启动", "星三角启动" });
            
            CreateCalcButton(container, CalculateMotorStartup);
            motorResult = CreateResultArea(container);
        }

        private void CalculateMotorStartup()
        {
            try
            {
                double pKw = double.Parse(motorPower.text);
                double u = double.Parse(motorVoltage.text);
                double cosPhi = double.Parse(motorCosPhi.text);
                double eta = double.Parse(motorEta.text);
                if (pKw <= 0 || u <= 0 || cosPhi <= 0 || eta <= 0) throw new Exception("参数必须大于 0");

                double pW = pKw * 1000;
                double iRated = pW / (1.7320508 * u * cosPhi * eta);
                
                double directMin = iRated * 5;
                double directMax = iRated * 7;

                string msg = $"估算结果：\n";
                msg += $"额定运行电流 I_rated ≈ {iRated:0.###} A\n";
                msg += $"直接启动电流估算范围：{directMin:0.###} A ～ {directMax:0.###} A\n";

                if (motorStartupType.value == 1)
                {
                    double starMin = directMin / 3.0;
                    double starMax = directMax / 3.0;
                    msg += $"星三角启动星形阶段电流估算：{starMin:0.###} A ～ {starMax:0.###} A\n";
                }
                
                msg += $"\n使用公式：\nI_rated = P / (√3 × U × cosφ × η)\n";
                msg += $"代入过程：\nI_rated = {pW} / (1.732 × {u} × {cosPhi} × {eta})\n\n";
                msg += "教学说明：\n直接启动电流通常是额定电流的5~7倍，适合小功率电机。星三角启动通过降低启动阶段绕组电压来减小启动电流，星形阶段线电流约为三角直接启动时的三分之一。\n";
                msg += "注意：可结合本软件内的“星三角降压启动”模板进行仿真验证。";
                
                ShowSuccess(motorResult, msg);
            }
            catch (Exception ex)
            {
                ShowError(motorResult, "参数填写错误。" + ex.Message);
            }
        }

        private void BuildMultiLoadPage(RectTransform page)
        {
            var container = CreateScrollableContent(page);
            CreateSectionTitle(container, "多负载回路总电流估算");
            
            multiSupplyType = CreateDropdown(container, "供电类型", new[] { "单相 220V", "三相 380V" });
            
            var row1 = CreateRow(container);
            multiLightCount = CreateInputFieldRaw(row1, "照明灯数量", "0");
            multiLightPower = CreateInputFieldRaw(row1, "单灯功率(W)", "0");

            var row2 = CreateRow(container);
            multiFanCount = CreateInputFieldRaw(row2, "风扇数量", "0");
            multiFanPower = CreateInputFieldRaw(row2, "单风扇功率(W)", "0");
            
            var row3 = CreateRow(container);
            multiMotorCount = CreateInputFieldRaw(row3, "电机数量", "0");
            multiMotorPower = CreateInputFieldRaw(row3, "单电机功率(kW)", "0");

            multiOtherPower = CreateInputField(container, "其他总功率 (W)", "0");
            multiCosPhi = CreateInputField(container, "回路平均功率因数", "默认0.8");
            multiCosPhi.text = "0.8";
            
            CreateCalcButton(container, CalculateMultiLoad);
            multiResult = CreateResultArea(container);
        }

        private void CalculateMultiLoad()
        {
            try
            {
                double GetVal(InputField f) => string.IsNullOrEmpty(f.text) ? 0 : double.Parse(f.text);
                
                double lCount = GetVal(multiLightCount); double lPower = GetVal(multiLightPower);
                double fCount = GetVal(multiFanCount); double fPower = GetVal(multiFanPower);
                double mCount = GetVal(multiMotorCount); double mPower = GetVal(multiMotorPower);
                double oPower = GetVal(multiOtherPower);
                double cosPhi = GetVal(multiCosPhi);
                
                if (cosPhi <= 0) cosPhi = 0.8;
                
                double totalPowerW = (lCount * lPower) + (fCount * fPower) + (mCount * mPower * 1000) + oPower;
                
                double u = multiSupplyType.value == 0 ? 220 : 380;
                double factor = multiSupplyType.value == 0 ? 1 : 1.7320508;
                
                double totalI = totalPowerW / (factor * u * cosPhi);
                double lI = (lCount * lPower) / (factor * u * 1.0); // light usually cosphi 1
                double fI = (fCount * fPower) / (factor * u * 0.8);
                double mI = (mCount * mPower * 1000) / (factor * u * 0.8 * 0.85);

                string msg = $"估算结果：\n";
                if(lCount > 0) msg += $"照明电流约 {lI:0.###} A\n";
                if(fCount > 0) msg += $"风扇电流约 {fI:0.###} A\n";
                if(mCount > 0) msg += $"电机电流约 {mI:0.###} A\n";
                msg += $"总估算电流 I_total ≈ {totalI:0.###} A\n\n";
                
                msg += $"代入过程：\n总功率 P_total = {totalPowerW} W\n";
                if (multiSupplyType.value == 0) msg += $"总电流 I = {totalPowerW} / (220 × {cosPhi})\n\n";
                else msg += $"总电流 I = {totalPowerW} / (1.732 × 380 × {cosPhi})\n\n";
                
                bool exceed = totalI > 16;
                msg += "判断提示：\n";
                msg += exceed ? "总电流较大（>16A），可能超过普通插座回路常见线径（2.5mm²）的范围！\n\n" : "总电流在普通回路常见范围内。\n\n";
                
                msg += "教学说明：\n多个负载并联接入同一回路时，总电流近似为各负载电流之和或总功率求得的总电流。\n建议结合“线径与空开估算”进一步判断线路是否安全。";
                
                ShowSuccess(multiResult, msg);
            }
            catch (Exception ex)
            {
                ShowError(multiResult, "参数填写错误。" + ex.Message);
            }
        }

        private void ShowError(Text uiText, string msg)
        {
            uiText.color = ErrorColor;
            uiText.text = "[错误] " + msg;
        }

        private void ShowSuccess(Text uiText, string msg)
        {
            uiText.color = TextDark;
            uiText.text = msg;
        }

        private RectTransform CreateRow(RectTransform parent)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 10;
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = 40f;
            return row.GetComponent<RectTransform>();
        }

        private RectTransform CreateScrollableContent(RectTransform parent)
        {
            var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scroll.transform.SetParent(parent, false);
            SetRect(scroll.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            scroll.GetComponent<Image>().color = new Color(0,0,0,0.01f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scroll.transform, false);
            SetRect(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            viewport.GetComponent<Image>().color = new Color(0,0,0,0.01f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 0);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scroll.GetComponent<ScrollRect>();
            sr.viewport = viewport.GetComponent<RectTransform>();
            sr.content = rect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;

            return rect;
        }

        private Text CreateSectionTitle(RectTransform parent, string title)
        {
            var txt = CreateText("SectionTitle", parent, title, 18, FontStyle.Bold, TextDark);
            var layoutElement = txt.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 32f;
            return txt;
        }

        private InputField CreateInputField(RectTransform parent, string labelText, string placeholderText = "")
        {
            var row = new GameObject("InputRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = 40f;

            var label = CreateText("Label", row.transform, labelText, 16, FontStyle.Normal, TextDark);
            var leLabel = label.gameObject.AddComponent<LayoutElement>();
            leLabel.preferredWidth = 140f;
            label.alignment = TextAnchor.MiddleLeft;

            var inputBg = new GameObject("InputBg", typeof(RectTransform), typeof(Image));
            inputBg.transform.SetParent(row.transform, false);
            inputBg.GetComponent<Image>().color = new Color(0.96f, 0.97f, 0.99f);
            var inputBorder = inputBg.AddComponent<Outline>();
            inputBorder.effectColor = new Color(0.8f, 0.82f, 0.86f);
            inputBorder.effectDistance = new Vector2(1, -1);
            var leInput = inputBg.AddComponent<LayoutElement>();
            leInput.preferredWidth = 240f;

            var inputText = CreateText("Text", inputBg.transform, "", 16, FontStyle.Normal, TextDark);
            SetRect(inputText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10, 0), new Vector2(-20, 0));
            inputText.alignment = TextAnchor.MiddleLeft;

            var inputField = inputBg.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.contentType = InputField.ContentType.DecimalNumber;

            if (!string.IsNullOrEmpty(placeholderText))
            {
                var ph = CreateText("Placeholder", inputBg.transform, placeholderText, 16, FontStyle.Italic, TextMuted);
                SetRect(ph.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10, 0), new Vector2(-20, 0));
                ph.alignment = TextAnchor.MiddleLeft;
                inputField.placeholder = ph;
            }

            return inputField;
        }

        private InputField CreateInputFieldRaw(RectTransform parent, string labelText, string placeholderText = "")
        {
            var row = new GameObject("InputRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            var label = CreateText("Label", row.transform, labelText, 15, FontStyle.Normal, TextDark);
            var leLabel = label.gameObject.AddComponent<LayoutElement>();
            leLabel.preferredWidth = 110f;
            label.alignment = TextAnchor.MiddleLeft;

            var inputBg = new GameObject("InputBg", typeof(RectTransform), typeof(Image));
            inputBg.transform.SetParent(row.transform, false);
            inputBg.GetComponent<Image>().color = new Color(0.96f, 0.97f, 0.99f);
            var inputBorder = inputBg.AddComponent<Outline>();
            inputBorder.effectColor = new Color(0.8f, 0.82f, 0.86f);
            inputBorder.effectDistance = new Vector2(1, -1);
            var leInput = inputBg.AddComponent<LayoutElement>();
            leInput.preferredWidth = 140f;

            var inputText = CreateText("Text", inputBg.transform, "", 15, FontStyle.Normal, TextDark);
            SetRect(inputText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(-16, 0));
            inputText.alignment = TextAnchor.MiddleLeft;

            var inputField = inputBg.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.contentType = InputField.ContentType.DecimalNumber;

            if (!string.IsNullOrEmpty(placeholderText))
            {
                var ph = CreateText("Placeholder", inputBg.transform, placeholderText, 15, FontStyle.Italic, TextMuted);
                SetRect(ph.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(-16, 0));
                ph.alignment = TextAnchor.MiddleLeft;
                inputField.placeholder = ph;
            }

            return inputField;
        }

        private Dropdown CreateDropdown(RectTransform parent, string labelText, string[] options)
        {
            var row = new GameObject("DropdownRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = 40f;

            var label = CreateText("Label", row.transform, labelText, 16, FontStyle.Normal, TextDark);
            var leLabel = label.gameObject.AddComponent<LayoutElement>();
            leLabel.preferredWidth = 140f;
            label.alignment = TextAnchor.MiddleLeft;

            return CreateBuiltinDropdown(row.transform, options, 240f);
        }

        private Dropdown CreateDropdownRaw(RectTransform parent, string labelText, string[] options)
        {
            var row = new GameObject("DropdownRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            var label = CreateText("Label", row.transform, labelText, 15, FontStyle.Normal, TextDark);
            var leLabel = label.gameObject.AddComponent<LayoutElement>();
            leLabel.preferredWidth = 110f;
            label.alignment = TextAnchor.MiddleLeft;

            return CreateBuiltinDropdown(row.transform, options, 140f);
        }

        private Dropdown CreateBuiltinDropdown(Transform parent, string[] options, float width)
        {
            GameObject go = UnityEngine.UI.DefaultControls.CreateDropdown(new UnityEngine.UI.DefaultControls.Resources());
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            var dropdown = go.GetComponent<Dropdown>();
            dropdown.options.Clear();
            foreach(var opt in options) dropdown.options.Add(new Dropdown.OptionData(opt));
            
            var label = go.transform.Find("Label").GetComponent<Text>();
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 15;
            label.color = TextDark;
            
            var template = go.transform.Find("Template");
            var itemLabel = template.Find("Viewport/Content/Item/Item Label").GetComponent<Text>();
            itemLabel.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabel.fontSize = 15;
            itemLabel.color = TextDark;
            
            return dropdown;
        }

        private Button CreateCalcButton(RectTransform parent, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject("CalcBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGo.transform.SetParent(parent, false);
            btnGo.GetComponent<Image>().color = PrimaryBlue;
            var le = btnGo.GetComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredWidth = 380f;
            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(action);

            var txt = CreateText("Text", btnGo.transform, "计算", 18, FontStyle.Bold, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            SetRect(txt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return btn;
        }

        private Text CreateResultArea(RectTransform parent)
        {
            var bg = new GameObject("ResultBg", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            bg.transform.SetParent(parent, false);
            bg.GetComponent<Image>().color = new Color(0.95f, 0.98f, 0.95f);
            var border = bg.AddComponent<Outline>();
            border.effectColor = new Color(0.7f, 0.9f, 0.7f);
            border.effectDistance = new Vector2(1, -1);

            var layout = bg.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);

            var txt = CreateText("ResultText", bg.transform, "等待输入计算...", 15, FontStyle.Normal, TextDark);
            txt.alignment = TextAnchor.UpperLeft;
            var le = txt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 120f;
            return txt;
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.lineSpacing = 1.3f;
            var csf = go.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return text;
        }

        private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private void ClearChildren(Transform parent)
        {
            foreach (Transform child in parent) Destroy(child.gameObject);
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
    }
}
