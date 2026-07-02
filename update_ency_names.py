import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Update ApplyOverride to NOT overwrite DisplayName
old_apply_override = '''            private static void ApplyOverride(ComponentEncyclopediaEntry target, ComponentEncyclopediaEntry source)
            {
                target.DisplayName = source.DisplayName;
                target.Category = source.Category;
                target.CircuitType = source.CircuitType;
                target.RatedVoltage = source.RatedVoltage;
                target.RatedCurrent = source.RatedCurrent;'''

new_apply_override = '''            private static void ApplyOverride(ComponentEncyclopediaEntry target, ComponentEncyclopediaEntry source)
            {
                // Do NOT overwrite DisplayName, let GetDisplayName handle it
                target.Category = source.Category;
                target.CircuitType = source.CircuitType;
                target.RatedVoltage = source.RatedVoltage;
                target.RatedCurrent = source.RatedCurrent;'''

content = content.replace(old_apply_override, new_apply_override)

# 2. Update GetDisplayName with specific naming logic
old_get_display_name = '''        private static string GetDisplayName(ComponentDefinition definition)
        {
            return definition == null || string.IsNullOrWhiteSpace(definition.displayName)
                ? definition != null ? definition.name : string.Empty
                : definition.displayName.Replace("\\n", " ");
        }'''

new_get_display_name = '''        private static string GetDisplayName(ComponentDefinition definition)
        {
            if (definition == null) return string.Empty;
            var name = definition.name;
            if (name.Contains("Contactor_KM_220V")) return "交流接触器（220V）";
            if (name.Contains("Contactor_KM_380V")) return "交流接触器（380V）";
            if (name.Contains("Timer_OnDelay_220V")) return "通电延时时间继电器（220V）";
            if (name.Contains("Timer_OnDelay_380V")) return "通电延时时间继电器（380V）";
            if (name.Contains("Timer_OffDelay")) return "断电延时时间继电器（待支持）";
            if (name.Contains("Indicator_Red_220V")) return "红色指示灯（220V）";
            if (name.Contains("Indicator_Green_220V")) return "绿色指示灯（220V）";
            if (name.Contains("Indicator_Yellow_220V")) return "黄色指示灯（220V）";
            if (name.Contains("Indicator_Red_380V")) return "红色指示灯（380V）";
            if (name.Contains("Indicator_Green_380V")) return "绿色指示灯（380V）";
            if (name.Contains("Indicator_Yellow_380V")) return "黄色指示灯（380V）";
            if (name.Contains("TerminalBlock_2H")) return "2位端子横排";
            if (name.Contains("TerminalBlock_2V")) return "2位端子竖排";
            if (name.Contains("TerminalBlock_3H")) return "3位端子横排";
            if (name.Contains("TerminalBlock_3V")) return "3位端子竖排";
            if (name.Contains("TerminalBlock_6H")) return "6位端子横排";
            if (name.Contains("TerminalBlock_6V")) return "6位端子竖排";
            if (name.Contains("AC_220V_Power")) return "220V交流电源";
            if (name.Contains("AC_ThreePhase_Power")) return "三相交流电源";
            if (name.Contains("Button_Start_NO")) return "启动按钮（NO）";
            if (name.Contains("Button_Stop_NC")) return "停止按钮（NC）";
            if (name.Contains("Button_Compound")) return "复合按钮（SB）";
            if (name.Contains("EmergencyStop")) return "急停按钮";
            if (name.Contains("Button_SelfLock")) return "自锁按钮";
            if (name.Contains("Tool_Multimeter")) return "万用表（无图）";

            return string.IsNullOrWhiteSpace(definition.displayName)
                ? definition.name
                : definition.displayName.Replace("\\n", " ").Replace(" (", "（").Replace(")", "）");
        }'''

content = content.replace(old_get_display_name, new_get_display_name)

# 3. Update BuildBasicSummary for specific parameter summaries
old_build_basic_summary = '''        private static string BuildBasicSummary(ComponentEncyclopediaEntry entry)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(entry.RatedVoltage))
            {
                parts.Add("电压 " + entry.RatedVoltage);
            }

            if (!string.IsNullOrWhiteSpace(entry.RatedCurrent))
            {
                parts.Add("电流 " + entry.RatedCurrent);
            }

            return parts.Count > 0 ? string.Join(" / ", parts) : "参数：待补充";
        }'''

new_build_basic_summary = '''        private static string BuildBasicSummary(ComponentEncyclopediaEntry entry)
        {
            var defName = entry.DefinitionName ?? "";
            if (defName.Contains("AC_220V_Power")) return "电压 220V";
            if (defName.Contains("AC_ThreePhase_Power")) return "电压 380V";
            if (defName.Contains("Button_Start_NO")) return "电压 220V / 电流 5A";
            if (defName.Contains("Button_Stop_NC")) return "电压 220V / 电流 5A";
            if (defName.Contains("Button_Compound")) return "电压 220V / 电流 5A";
            if (defName.Contains("EmergencyStop")) return "电压 220V / 常闭触点";
            if (defName.Contains("Single_Control_Switch")) return "电压 220V / 电流 5A";
            if (defName.Contains("Two_Way_Switch")) return "电压 220V / 电流 5A";
            if (defName.Contains("Contactor_KM_220V")) return "线圈 220V";
            if (defName.Contains("Contactor_KM_380V")) return "线圈 380V";
            if (defName.Contains("ThermalRelay")) return "过载保护 / 95-96 NC";
            if (defName.Contains("Timer_OnDelay")) return "电压 220V / 延时触点";
            if (defName.Contains("LimitSwitch")) return "电压 220V / NO+NC";
            if (defName.Contains("KnifeSwitch")) return "电压 380V / 电流 32A";
            if (defName.Contains("Breaker")) return "按极数配置 / 保护开关";
            if (defName.Contains("Fuse")) return "过流保护 / 熔断断开";
            if (defName.Contains("Motor_ThreePhase")) return "电压 380V";
            if (defName.Contains("Motor_StarDelta")) return "电压 380V / 六端子";
            if (defName.Contains("Lamp")) return "电压 220V";
            if (defName.Contains("Fan")) return "电压 220V";
            if (defName.Contains("Single_Phase_Meter")) return "电压 220V";
            if (defName.Contains("Indicator_")) return "按电压和颜色区分";
            if (defName.Contains("TerminalBlock")) return "按位导通";
            if (defName.Contains("Tool_Multimeter")) return "测量工具 / 图片待补充";
            if (defName.Contains("Button_SelfLock")) return "电压 220V / 电流 5A";
            if (defName.Contains("Timer_OffDelay")) return "待支持 / 延时触点";

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(entry.RatedVoltage)) parts.Add("电压 " + entry.RatedVoltage);
            if (!string.IsNullOrWhiteSpace(entry.RatedCurrent)) parts.Add("电流 " + entry.RatedCurrent);
            return parts.Count > 0 ? string.Join(" / ", parts) : "参数：待补充";
        }'''

content = content.replace(old_build_basic_summary, new_build_basic_summary)

# 4. Update Search matching
old_search_logic = '''        private void OnSearchValueChanged(string query)
        {
            var isSearching = !string.IsNullOrWhiteSpace(query);
            foreach (var card in cards)
            {
                var entry = card.Key;
                var match = string.IsNullOrWhiteSpace(query) ||
                            MatchesSearch(entry.DisplayName, query) ||
                            MatchesSearch(entry.Category, query) ||
                            MatchesSearch(entry.Purpose, query);
                card.Value.gameObject.SetActive(match);
            }
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(listViewRoot);
            UpdateCategorySelection();
        }

        private bool MatchesSearch(string value, string query)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   !string.IsNullOrWhiteSpace(query) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }'''

new_search_logic = '''        private void OnSearchValueChanged(string query)
        {
            foreach (var card in cards)
            {
                var match = MatchesSearchEntry(card.Key, query);
                card.Value.gameObject.SetActive(match);
            }
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(listViewRoot);
            UpdateCategorySelection();
        }

        private bool MatchesSearchEntry(ComponentEncyclopediaEntry entry, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;

            if (MatchesSearch(entry.DisplayName, query)) return true;
            if (MatchesSearch(entry.Category, query)) return true;
            if (MatchesSearch(entry.Purpose, query)) return true;

            var defName = entry.DefinitionName ?? "";
            var aliases = "";
            if (defName.Contains("Contactor")) aliases = "接触器 KM Contactor";
            else if (defName.Contains("Timer_")) aliases = "KT 时间继电器 延时继电器";
            else if (defName.Contains("ThermalRelay")) aliases = "FR 热继 过载保护";
            else if (defName.Contains("LimitSwitch")) aliases = "SQ 限位开关 行程开关";
            else if (defName.Contains("EmergencyStop")) aliases = "急停 急停开关 Emergency Stop";
            else if (defName.Contains("Motor_")) aliases = "电机 三相电动机 Motor";
            else if (defName.Contains("TerminalBlock")) aliases = "端子 接线端子 Terminal Block";
            else if (defName.Contains("Indicator_")) aliases = "信号灯 运行灯 报警灯";

            return MatchesSearch(aliases, query);
        }

        private bool MatchesSearch(string value, string query)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   !string.IsNullOrWhiteSpace(query) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }'''

content = content.replace(old_search_logic, new_search_logic)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Finished applying C# fixes")
