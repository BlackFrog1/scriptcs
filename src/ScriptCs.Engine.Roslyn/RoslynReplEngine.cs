﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Roslyn.Scripting;
using ScriptCs.Contracts;
using ScriptCs.Logging;

namespace ScriptCs.Engine.Roslyn
{
    using System.Globalization;

    public class RoslynReplEngine : RoslynScriptEngine, IReplEngine
    {
        public RoslynReplEngine(IScriptHostFactory scriptHostFactory, ILog logger)
            : base(scriptHostFactory, logger)
        {
        }

        public System.Type GetLocalVariableType(ScriptPackSession scriptPackSession, string variableName)
        {
            // borrow some code from GetLocalVariables
            if (scriptPackSession != null && scriptPackSession.State.ContainsKey(SessionKey))
            {
                var sessionState = (SessionState<Session>)scriptPackSession.State[SessionKey];
                var submissionObjectField = sessionState.Session.GetType()
                    .GetField("submissions", BindingFlags.Instance | BindingFlags.NonPublic);

                if (submissionObjectField != null)
                {
                    var submissionObjectFieldValue = submissionObjectField.GetValue(sessionState.Session);
                    if (submissionObjectFieldValue != null)
                    {
                        var submissionObjects = submissionObjectFieldValue as object[];

                        if (submissionObjects != null && submissionObjects.Any(x => x != null))
                        {
                            // reversing to get the latest submission first
                            foreach (var submissionObject in submissionObjects.Where(x => x != null).Reverse())
                            {
                                foreach (var field in submissionObject.GetType().GetFields()
                                    .Where(x => x.Name.ToLowerInvariant() != "<host-object>")
                                    .Where(field => string.Compare(field.Name, variableName) == 0))
                                {
                                    return field.FieldType;
                                }
                            }
                        }
                    }

                }
            }

            return null;
        }

        public ICollection<string> GetLocalVariables(ScriptPackSession scriptPackSession)
        {
            var variables = new Collection<string>();
            if (scriptPackSession != null && scriptPackSession.State.ContainsKey(SessionKey))
            {
                var sessionState = (SessionState<Session>)scriptPackSession.State[SessionKey];
                var submissionObjectField = sessionState.Session.GetType()
                    .GetField("submissions", BindingFlags.Instance | BindingFlags.NonPublic);

                if (submissionObjectField != null)
                {
                    var submissionObjectFieldValue = submissionObjectField.GetValue(sessionState.Session);
                    if (submissionObjectFieldValue != null)
                    {
                        var submissionObjects = submissionObjectFieldValue as object[];

                        if (submissionObjects != null && submissionObjects.Any(x => x != null))
                        {
                            var processedFields = new Collection<string>();

                            // reversing to get the latest submission first
                            foreach (var submissionObject in submissionObjects.Where(x => x != null).Reverse())
                            {
                                foreach (var field in submissionObject.GetType().GetFields()
                                    .Where(x => x.Name.ToLowerInvariant() != "<host-object>")
                                    .Where(field => !processedFields.Contains(field.Name)))
                                {
                                    var variable = string.Format(
                                        CultureInfo.InvariantCulture,
                                        "{0} {1} = {2}",
                                        field.FieldType,
                                        field.Name,
                                        field.GetValue(submissionObject));

                                    variables.Add(variable);
                                    processedFields.Add(field.Name);
                                }
                            }
                        }
                    }

                }
            }

            return variables;
        }

        protected override ScriptResult Execute(string code, Session session)
        {
            Guard.AgainstNullArgument("session", session);

            return string.IsNullOrWhiteSpace(FileName) && !IsCompleteSubmission(code)
                ? ScriptResult.Incomplete
                : base.Execute(code, session);
        }
    }
}