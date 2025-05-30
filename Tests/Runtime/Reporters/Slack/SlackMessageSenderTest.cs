// Copyright (c) 2023 DeNA Co., Ltd.
// This software is released under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeNA.Anjin.TestDoubles;
using NUnit.Framework;
using TestHelper.Attributes;
using UnityEngine;

namespace DeNA.Anjin.Reporters.Slack
{
    [TestFixture]
    public class SlackMessageSenderTest
    {
        [Test]
        public async Task NoMentionsAndNoScreenshotsAndNoAtHere()
        {
            var spySlackAPI = new SpySlackAPI();
            var sender = new SlackMessageSender(spySlackAPI);

            await sender.Send(
                "TOKEN",
                "CHANNEL",
                Array.Empty<string>(),
                false,
                "LEAD",
                "MESSAGE",
                "STACKTRACE",
                Color.magenta,
                false
            );

            var actual = spySlackAPI.Arguments;
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" },
                    { "channel", "CHANNEL" },
                    { "text", "LEAD" },
                    { "message", "MESSAGE" },
                    { "color", "RGBA(1.000, 0.000, 1.000, 1.000)" },
                    { "ts", null }
                },
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" }, //
                    { "channel", "CHANNEL" },
                    { "text", "STACKTRACE" },
                    { "ts", "1" }
                },
            };
            Assert.That(actual, Is.EqualTo(expected), Format(actual));
        }

        [Test]
        public async Task WithMention()
        {
            var spySlackAPI = new SpySlackAPI();
            var sender = new SlackMessageSender(spySlackAPI);

            await sender.Send(
                "TOKEN",
                "CHANNEL",
                new[] { "MENTION1", "MENTION2" },
                false,
                "LEAD",
                "MESSAGE",
                "STACKTRACE",
                Color.magenta,
                false
            );

            var actual = spySlackAPI.Arguments;
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" },
                    { "channel", "CHANNEL" },
                    { "text", "<!subteam^MENTION1> <!subteam^MENTION2> LEAD" },
                    { "message", "MESSAGE" },
                    { "color", "RGBA(1.000, 0.000, 1.000, 1.000)" },
                    { "ts", null }
                },
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" }, // 
                    { "channel", "CHANNEL" },
                    { "text", "STACKTRACE" },
                    { "ts", "1" }
                },
            };
            Assert.That(actual, Is.EqualTo(expected), Format(actual));
        }

        [Test]
        [FocusGameView]
        public async Task WithScreenshot()
        {
            var spySlackAPI = new SpySlackAPI();
            var sender = new SlackMessageSender(spySlackAPI);

            await sender.Send(
                "TOKEN",
                "CHANNEL",
                new[] { "MENTION1", "MENTION2" },
                false,
                "LEAD",
                "MESSAGE",
                "STACKTRACE",
                Color.magenta,
                true
            );

            var actual = spySlackAPI.Arguments;
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" },
                    { "channel", "CHANNEL" },
                    { "text", "<!subteam^MENTION1> <!subteam^MENTION2> LEAD" },
                    { "message", "MESSAGE" },
                    { "color", "RGBA(1.000, 0.000, 1.000, 1.000)" },
                    { "ts", null }
                },
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" },
                    { "channel", "CHANNEL" },
                    { "text", null },
                    { "message", "IMAGE" },
                    { "color", "RGBA(0.000, 0.000, 0.000, 0.000)" }, // default
                    { "ts", "1" }
                },
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" }, // 
                    { "channel", "CHANNEL" },
                    { "text", "STACKTRACE" },
                    { "ts", "1" }
                },
            };
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task WithAtHere()
        {
            var spySlackAPI = new SpySlackAPI();
            var sender = new SlackMessageSender(spySlackAPI);

            await sender.Send(
                "TOKEN",
                "CHANNEL",
                Array.Empty<string>(),
                true,
                "LEAD",
                "MESSAGE",
                "STACKTRACE",
                Color.magenta,
                false
            );

            var actual = spySlackAPI.Arguments;
            var expected = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" },
                    { "channel", "CHANNEL" },
                    { "text", "<!here> LEAD" },
                    { "message", "MESSAGE" },
                    { "color", "RGBA(1.000, 0.000, 1.000, 1.000)" },
                    { "ts", null }
                },
                new Dictionary<string, string>
                {
                    { "token", "TOKEN" }, //
                    { "channel", "CHANNEL" },
                    { "text", "STACKTRACE" },
                    { "ts", "1" }
                },
            };
            Assert.That(actual, Is.EqualTo(expected), Format(actual));
        }

        private static string Format(List<Dictionary<string, string>> dicts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");
            foreach (var dict in dicts)
            {
                sb.AppendLine("\t{");
                var keys = dict.Keys.ToArray();
                Array.Sort(keys);
                foreach (var key in keys)
                {
                    var value = dict[key];
                    sb.Append("\t\t");
                    sb.Append(key);
                    sb.Append(": ");
                    sb.Append(value);
                    sb.AppendLine(",");
                }

                sb.AppendLine("\t},");
            }

            sb.AppendLine("]");
            return sb.ToString();
        }
    }
}
