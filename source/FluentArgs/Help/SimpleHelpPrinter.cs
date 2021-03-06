﻿namespace FluentArgs.Help
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentArgs.Extensions;

    public class SimpleHelpPrinter : IHelpPrinter
    {
        private const string Tab = "    ";
        private const int MaxLineLength = 80;
        private readonly IList<(string parameterName, string description)> parameters;
        private readonly ILineWriter outputWriter;

        public SimpleHelpPrinter(TextWriter outputWriter)
        {
            this.outputWriter = new LineWriter(outputWriter);
            parameters = new List<(string, string)>();
        }

        public async Task WriteApplicationDescription(string description)
        {
            await outputWriter.WriteLines(SplitLine(description, MaxLineLength)).ConfigureAwait(false);
            await outputWriter.WriteLine(string.Empty).ConfigureAwait(false);
        }

        public Task WriteParameterInfos(
            IReadOnlyCollection<string> aliases,
            string? description,
            Type type,
            bool optional,
            bool hasDefaultValue,
            object? defaultValue,
            IReadOnlyCollection<string> examples,
            IReadOnlyCollection<(IReadOnlyCollection<string> aliases, string description)> givenHints)
        {
            var aliasStr = aliases.StringifyAliases();
            var descriptionStr = string.Empty;

            if (optional)
            {
                if (hasDefaultValue)
                {
                    descriptionStr = $"Optional with default '{defaultValue}'. ";
                }
                else
                {
                    descriptionStr = "Optional. ";
                }
            }

            if (givenHints.Count > 0)
            {
                descriptionStr += GetGivenHintsOutput(givenHints);
            }

            if (description != null)
            {
                descriptionStr += description + " ";
            }
            else
            {
                descriptionStr += $"Type: {type.Name} ";
            }

            if (examples.Count > 0)
            {
                descriptionStr += "Examples: " + string.Join(", ", examples);
            }
            else if (type.IsEnum)
            {
                descriptionStr += "Possible values: " + string.Join(", ", Enum.GetValues(type).Cast<object>().ToArray());
            }

            parameters.Add((aliasStr, descriptionStr));

            return Task.CompletedTask;
        }

        public Task WriteListParameterInfos(
            IReadOnlyCollection<string> aliases,
            string? description,
            Type type,
            bool optional,
            IReadOnlyCollection<string> separators,
            bool hasDefaultValue,
            object? defaultValue,
            IReadOnlyCollection<string> examples,
            IReadOnlyCollection<(IReadOnlyCollection<string> aliases, string description)> givenHints)
        {
            var aliasStr = aliases.StringifyAliases(); // TODO: Test stringify aliases
            var descriptionStr = string.Empty;
            if (optional)
            {
                if (hasDefaultValue)
                {
                    descriptionStr = $"Optional with default '{defaultValue}'. ";
                }
                else
                {
                    descriptionStr = "Optional. ";
                }
            }

            if (description != null)
            {
                descriptionStr += description + " ";
            }
            else
            {
                descriptionStr += $"Type: {type.Name} ";
            }

            if (examples.Count > 0)
            {
                descriptionStr += "Examples: " + string.Join(", ", examples);
            }
            else if (type.IsEnum)
            {
                descriptionStr += "Possible values: " + string.Join(", ", Enum.GetValues(type).Cast<object>().ToArray()) + ". ";
            }

            if (separators.Count == 0)
            {
                throw new Exception("TODO");
            }

            descriptionStr += "Multiple values can be used by joining them with ";
            if (separators.Count == 1)
            {
                descriptionStr += $"the separator '{separators.First()}'.";
            }
            else
            {
                descriptionStr += $"any of the following separators: {string.Join(" ", separators)}";
            }

            parameters.Add((aliasStr, descriptionStr));

            return Task.CompletedTask;
        }

        public Task WritePositionalArgumentInfos(
            string? description,
            Type type,
            bool optional,
            bool hasDefaultValue,
            object? defaultValue,
            IReadOnlyCollection<string> examples,
            IReadOnlyCollection<(IReadOnlyCollection<string> aliases, string description)> givenHints)
        {
            var descriptionStr = "Positional argument. ";

            if (optional)
            {
                if (hasDefaultValue)
                {
                    descriptionStr = $"Optional with default '{defaultValue}'. ";
                }
                else
                {
                    descriptionStr = "Optional. ";
                }
            }

            if (givenHints.Count > 0)
            {
                descriptionStr += GetGivenHintsOutput(givenHints);
            }

            if (description != null)
            {
                descriptionStr += description + " ";
            }
            else
            {
                descriptionStr += $"Type: {type.Name} ";
            }

            if (examples.Count > 0)
            {
                descriptionStr += "Examples: " + string.Join(", ", examples);
            }
            else if (type.IsEnum)
            {
                descriptionStr += "Possible values: " + string.Join(", ", Enum.GetValues(type).Cast<object>().ToArray());
            }

            parameters.Add(("[ARG]", descriptionStr));

            return Task.CompletedTask;
        }

        public async Task Finalize()
        {
            if (parameters.Count == 0)
            {
                return;
            }

            var maxNameLength = parameters.Max(p => p.parameterName.Length);
            if (maxNameLength > 25)
            {
                foreach (var parameter in parameters)
                {
                    await outputWriter.WriteLines(SplitLine(parameter.parameterName, MaxLineLength)).ConfigureAwait(false);
                    await outputWriter.WriteLines(SplitLine(parameter.description, MaxLineLength - Tab.Length).Select(l => $"{Tab}{l}")).ConfigureAwait(false);
                }
            }
            else
            {
                var separator = " ";
                var descriptionLength = MaxLineLength - maxNameLength - separator.Length;
                var linesPrefix = string.Concat(Enumerable.Repeat(" ", maxNameLength + separator.Length));

                await outputWriter.WriteLines(parameters.SelectMany(p =>
                {
                    var lines = SplitLine(p.description, descriptionLength).ToArray();
                    var firstLine = p.parameterName.PadRight(maxNameLength) + separator + lines.First();
                    return lines.Skip(1).Select(l => linesPrefix + l).Prepend(firstLine);
                })).ConfigureAwait(false);
            }
        }

        public Task WriteFlagInfos(IReadOnlyCollection<string> aliases, string? description, IReadOnlyCollection<(IReadOnlyCollection<string> aliases, string description)> givenHints)
        {
            var aliasStr = aliases.StringifyAliases();
            var descriptionStr = string.Empty;
            if (givenHints.Count > 0)
            {
                descriptionStr += GetGivenHintsOutput(givenHints);
            }

            descriptionStr += description ?? "A flag";

            parameters.Add((aliasStr, descriptionStr));
            return Task.CompletedTask;
        }

        public Task WriteRemainingArgumentsAreUsed(string? description, Type type, IReadOnlyCollection<string> examples, IReadOnlyCollection<(IReadOnlyCollection<string> aliases, string description)> givenHints)
        {
            var descriptionStr = string.Empty;
            if (givenHints.Count > 0)
            {
                descriptionStr += GetGivenHintsOutput(givenHints);
            }

            if (description != null)
            {
                descriptionStr += description + " ";
            }
            else
            {
                descriptionStr += $"All remaining arguments are parsed. Type: {type.Name} ";
            }

            if (examples.Count > 0)
            {
                descriptionStr += "Examples: " + string.Join(", ", examples);
            }
            else if (type.IsEnum)
            {
                descriptionStr += "Possible values: " + string.Join(", ", Enum.GetValues(type).Cast<object>().ToArray()) + ". ";
            }

            parameters.Add(("[...]", descriptionStr));
            return Task.CompletedTask;
        }

        private static IEnumerable<string> SplitLine(string line, int maxLineLength)
        {
            while (line.Length > maxLineLength)
            {
                var spaceIndices = line
                    .Take(maxLineLength)
                    .Select((c, i) => (character: c, index: i))
                    .Where(c => c.character == ' ')
                    .Select(c => c.index)
                    .ToList();

                var charsForCurrentLine = maxLineLength;
                if (spaceIndices.Count > 0)
                {
                    charsForCurrentLine = spaceIndices.Last() + 1;
                }

                yield return string.Concat(line.Take(charsForCurrentLine));
                line = string.Concat(line.Skip(charsForCurrentLine));
            }

            if (line.Length > 0)
            {
                yield return line;
            }
        }

        private static string GetGivenHintsOutput(IReadOnlyCollection<(IReadOnlyCollection<string> aliases, string description)> givenHints)
        {
            var descriptions = givenHints.Reverse().Select(h => $"{h.aliases.OrderBy(a => a.Length).First()} {h.description}").ToArray();
            var descriptionStr = "Only available if ";
            if (descriptions.Length > 1)
            {
                descriptionStr += $"{string.Join(", ", descriptions.Take(descriptions.Length - 1))} and {descriptions.Last()}. ";
            }
            else
            {
                descriptionStr += $"{descriptions.First()}. ";
            }

            return descriptionStr;
        }
    }
}
