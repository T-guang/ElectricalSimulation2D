using System.Collections.Generic;
using ElectricalSim.Core;
using ElectricalSim.Templates;

namespace ElectricalSim.Practice.Netlist
{
    public static class PracticeConnectionChecker
    {
        public static PracticeConnectionCheckResult Check(WorkspaceController workspace, CircuitTemplateDto template)
        {
            var result = new PracticeConnectionCheckResult();
            if (workspace == null || template == null)
            {
                result.Passed = false;
                result.MissingConnections.Add(new PracticeConnectionIssue(PracticeConnectionIssueKind.MissingConnection, "\u5f53\u524d\u6ca1\u6709\u53ef\u7528\u4e8e\u5224\u5b9a\u7684\u7ec3\u4e60\u6a21\u677f\u3002"));
                return result;
            }

            var standard = StandardNetlistBuilder.Build(template);
            var student = StudentNetlistBuilder.Build(workspace);
            var mapping = ComponentMappingSolver.Solve(standard, student);

            foreach (var message in mapping.MissingComponentMessages)
            {
                result.MissingComponents.Add(new PracticeConnectionIssue(PracticeConnectionIssueKind.MissingComponent, message));
            }

            foreach (var message in mapping.ExtraComponentMessages)
            {
                result.ExtraComponents.Add(new PracticeConnectionIssue(PracticeConnectionIssueKind.ExtraComponent, message));
            }

            if (mapping.Ambiguous)
            {
                result.AmbiguousMapping = true;
            }

            AddMissingConnections(result, standard, student, mapping.StandardToStudent);
            AddWrongDirectConnections(result, standard, student, mapping.StudentToStandard);
            AddExtraNodeMerges(result, standard, student, mapping.StudentToStandard);
            AddSuggestions(result, standard);

            result.Passed = !result.HasIssues;
            return result;
        }

        private static void AddMissingConnections(PracticeConnectionCheckResult result, PracticeNetlist standard, PracticeNetlist student, Dictionary<string, string> standardToStudent)
        {
            var reported = new HashSet<string>();
            foreach (var connection in standard.DirectConnections)
            {
                if (!ComponentMappingSolver.TryMapTerminal(connection.StartKey, standardToStudent, out var mappedStart) ||
                    !ComponentMappingSolver.TryMapTerminal(connection.EndKey, standardToStudent, out var mappedEnd))
                {
                    var key = connection.GetUndirectedKey();
                    if (reported.Add(key))
                    {
                        result.MissingConnections.Add(new PracticeConnectionIssue(
                            PracticeConnectionIssueKind.MissingConnection,
                            "\u7f3a\u5c11\u8fde\u63a5\uff1a" + standard.DescribeTerminal(connection.StartKey) + " \u5e94\u8fde\u901a\u5230 " + standard.DescribeTerminal(connection.EndKey) + "\u3002"));
                    }

                    continue;
                }

                if (!student.AreConnected(mappedStart, mappedEnd))
                {
                    var key = mappedStart + "<->" + mappedEnd;
                    if (reported.Add(key))
                    {
                        result.MissingConnections.Add(new PracticeConnectionIssue(
                            PracticeConnectionIssueKind.MissingConnection,
                            "\u7f3a\u5c11\u8fde\u63a5\uff1a" + student.DescribeTerminal(mappedStart) + " \u5e94\u8fde\u901a\u5230 " + student.DescribeTerminal(mappedEnd) + "\u3002"));
                    }
                }
            }
        }

        private static void AddWrongDirectConnections(PracticeConnectionCheckResult result, PracticeNetlist standard, PracticeNetlist student, Dictionary<string, string> studentToStandard)
        {
            var reported = new HashSet<string>();
            foreach (var connection in student.DirectConnections)
            {
                if (!ComponentMappingSolver.TryMapTerminal(connection.StartKey, studentToStandard, out var mappedStart) ||
                    !ComponentMappingSolver.TryMapTerminal(connection.EndKey, studentToStandard, out var mappedEnd))
                {
                    var key = connection.GetUndirectedKey();
                    if (reported.Add(key))
                    {
                        result.WrongConnections.Add(new PracticeConnectionIssue(
                            PracticeConnectionIssueKind.WrongConnection,
                            "\u7aef\u5b50\u63a5\u9519\uff1a" + student.DescribeTerminal(connection.StartKey) + " \u4e0e " + student.DescribeTerminal(connection.EndKey) + " \u4e0d\u5c5e\u4e8e\u672c\u7ec3\u4e60\u9700\u8981\u7684\u6807\u51c6\u8fde\u63a5\u3002"));
                    }

                    continue;
                }

                if (!standard.AreConnected(mappedStart, mappedEnd))
                {
                    var key = connection.GetUndirectedKey();
                    if (reported.Add(key))
                    {
                        result.WrongConnections.Add(new PracticeConnectionIssue(
                            PracticeConnectionIssueKind.WrongConnection,
                            "\u7aef\u5b50\u63a5\u9519\uff1a" + student.DescribeTerminal(connection.StartKey) + " \u4e0d\u5e94\u8fde\u901a\u5230 " + student.DescribeTerminal(connection.EndKey) + "\u3002"));
                    }
                }
            }
        }

        private static void AddExtraNodeMerges(PracticeConnectionCheckResult result, PracticeNetlist standard, PracticeNetlist student, Dictionary<string, string> studentToStandard)
        {
            var reported = new HashSet<string>();
            foreach (var group in student.GetEquivalentNodeGroups())
            {
                for (var i = 0; i < group.Count; i++)
                {
                    for (var j = i + 1; j < group.Count; j++)
                    {
                        var studentA = group[i];
                        var studentB = group[j];
                        if (!ComponentMappingSolver.TryMapTerminal(studentA, studentToStandard, out var standardA) ||
                            !ComponentMappingSolver.TryMapTerminal(studentB, studentToStandard, out var standardB))
                        {
                            continue;
                        }

                        if (standard.AreConnected(standardA, standardB))
                        {
                            continue;
                        }

                        var key = string.CompareOrdinal(studentA, studentB) <= 0 ? studentA + "<->" + studentB : studentB + "<->" + studentA;
                        if (reported.Add(key))
                        {
                            result.ExtraConnections.Add(new PracticeConnectionIssue(
                                PracticeConnectionIssueKind.ExtraConnection,
                                "\u591a\u4f59\u8fde\u63a5\uff1a" + student.DescribeTerminal(studentA) + " \u4e0e " + student.DescribeTerminal(studentB) + " \u88ab\u63a5\u5230\u4e86\u540c\u4e00\u7535\u6c14\u8282\u70b9\uff0c\u4f46\u6807\u51c6\u7b54\u6848\u4e2d\u5b83\u4eec\u4e0d\u5e94\u8fde\u901a\u3002"));
                        }
                    }
                }
            }
        }

        private static void AddSuggestions(PracticeConnectionCheckResult result, PracticeNetlist standard)
        {
            foreach (var connection in standard.DirectConnections)
            {
                result.CorrectConnectionSuggestions.Add(new PracticeConnectionIssue(
                    PracticeConnectionIssueKind.Suggestion,
                    standard.DescribeTerminal(connection.StartKey) + " -> " + standard.DescribeTerminal(connection.EndKey)));
            }
        }
    }
}
