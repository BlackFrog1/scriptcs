using System.Collections.Generic;

namespace ScriptCs.Contracts
{
    public interface IReplEngine : IScriptEngine
    {
        System.Type GetLocalVariableType(ScriptPackSession scriptPackSession, string variableName);

        ICollection<string> GetLocalVariables(ScriptPackSession scriptPackSession);
    }
}