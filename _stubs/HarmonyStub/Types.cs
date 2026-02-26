#pragma warning disable CS0626, CS0824, CS0114, CS0108, CS0067, CS0649, CS0169, CS0414, CS0109
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = true)]
    public class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type declaringType) { }
        public HarmonyPatch(Type declaringType, string methodName) { }
        public HarmonyPatch(Type declaringType, string methodName, params Type[] argumentTypes) { }
        public HarmonyPatch(Type declaringType, string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations) { }
        public HarmonyPatch(Type declaringType, MethodType methodType) { }
        public HarmonyPatch(string methodName) { }
        public HarmonyPatch(string methodName, params Type[] argumentTypes) { }
        public HarmonyPatch(string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations) { }
        public HarmonyPatch(MethodType methodType) { }
        public HarmonyPatch(string methodName, MethodType methodType) { }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HarmonyPatchAll : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyPostfix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyTranspiler : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyFinalizer : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyReversePatch : Attribute { public HarmonyReversePatch(HarmonyReversePatchType type = HarmonyReversePatchType.Original) { } }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyPrepare : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyCleanup : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyTargetMethod : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyTargetMethods : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Class)]
    public class HarmonyArgument : Attribute { public HarmonyArgument(string name) { } public HarmonyArgument(int index) { } public HarmonyArgument(string name, string newName) { } public HarmonyArgument(int index, string name) { } }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyAfter : Attribute { public HarmonyAfter(params string[] after) { } }
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyBefore : Attribute { public HarmonyBefore(params string[] before) { } }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HarmonyPriority : Attribute { public HarmonyPriority(int priority) { } }
    public enum HarmonyReversePatchType { Original, Snapshot }
    public enum HarmonyPatchType { All, Prefix, Postfix, Transpiler, Finalizer, ReversePatch, ILManipulator }
    public enum MethodType { Normal, Getter, Setter, Constructor, StaticConstructor, Enumerator, Async }
    public enum ArgumentType { Normal, Ref, Out, Pointer }

    public class Harmony
    {
        public string Id { get; }
        public Harmony(string id) { Id = id; }
        public void PatchAll() { }
        public void PatchAll(Assembly assembly) { }
        public MethodInfo Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null) => null;
        public void Unpatch(MethodBase original, HarmonyPatchType type, string harmonyID = null) { }
        public void UnpatchAll(string harmonyID = null) { }
    }

    public class HarmonyMethod
    {
        public MethodInfo method;
        public int priority;
        public string[] before;
        public string[] after;
        public HarmonyMethod() { }
        public HarmonyMethod(MethodInfo method) { this.method = method; }
        public HarmonyMethod(Type type, string name, Type[] parameters = null) { }
    }

    public static class AccessTools
    {
        public static FieldInfo Field(Type type, string name) => null;
        public static PropertyInfo Property(Type type, string name) => null;
        public static MethodInfo Method(Type type, string name, Type[] parameters = null, Type[] generics = null) => null;
        public static MethodInfo DeclaredMethod(Type type, string name, Type[] parameters = null, Type[] generics = null) => null;
        public static ConstructorInfo Constructor(Type type, Type[] parameters = null, bool searchForStatic = false) => null;
        public static Type TypeByName(string name) => null;
        public static Type Inner(Type type, string name) => null;
        public static List<FieldInfo> GetDeclaredFields(Type type) => null;
        public static List<PropertyInfo> GetDeclaredProperties(Type type) => null;
        public static List<MethodInfo> GetDeclaredMethods(Type type) => null;
        public static FieldInfo FieldRefAccess<T, F>(string fieldName) => null;
        public delegate ref F FieldRef<T, F>(T obj);
        public static FieldRef<T, F> FieldRefAccess<T, F>(FieldInfo fieldInfo) => null;
        public static T CreateInstance<T>() => default;
        public static object CreateInstance(Type type) => null;
        public static MethodInfo FirstMethod(Type type, Func<MethodInfo, bool> predicate) => null;
        public static FieldInfo DeclaredField(Type type, string name) => null;
        public static PropertyInfo DeclaredProperty(Type type, string name) => null;
        public static List<string> GetFieldNames(Type type) => null;
        public static List<string> GetPropertyNames(Type type) => null;
    }

    public class CodeInstruction
    {
        public OpCode opcode;
        public object operand;
        public List<System.Reflection.Emit.Label> labels;
        public List<ExceptionBlock> blocks;
        public CodeInstruction() { labels = new List<System.Reflection.Emit.Label>(); blocks = new List<ExceptionBlock>(); }
        public CodeInstruction(OpCode opcode, object operand = null) { this.opcode = opcode; this.operand = operand; labels = new List<System.Reflection.Emit.Label>(); blocks = new List<ExceptionBlock>(); }
        public CodeInstruction(CodeInstruction instruction) { opcode = instruction.opcode; operand = instruction.operand; labels = new List<System.Reflection.Emit.Label>(instruction.labels); blocks = new List<ExceptionBlock>(instruction.blocks); }
        public bool Is(OpCode opcode, object operand = null) => false;
        public bool IsStloc(object local = null) => false;
        public bool IsLdloc(object local = null) => false;
        public bool Calls(MethodInfo method) => false;
        public bool LoadsField(FieldInfo field, bool byAddress = false) => false;
        public bool StoresField(FieldInfo field) => false;
        public CodeInstruction Clone() => new CodeInstruction(this);
        public override string ToString() => "";
    }
    public class ExceptionBlock { public ExceptionBlockType blockType; public Type catchType; }
    public enum ExceptionBlockType { BeginExceptionBlock, BeginCatchBlock, BeginExceptFilterBlock, BeginFaultBlock, BeginFinallyBlock, EndExceptionBlock }

    public static class Transpilers { public static IEnumerable<CodeInstruction> MethodReplacer(IEnumerable<CodeInstruction> instructions, MethodInfo from, MethodInfo to) => null; }

    public static class Code
    {
        public static OpCode Ret => OpCodes.Ret;
        public static OpCode Nop => OpCodes.Nop;
        public static OpCode Ldarg_0 => OpCodes.Ldarg_0;
        public static OpCode Ldarg_1 => OpCodes.Ldarg_1;
        public static OpCode Ldarg_2 => OpCodes.Ldarg_2;
        public static OpCode Ldarg_3 => OpCodes.Ldarg_3;
        public static OpCode Call => OpCodes.Call;
        public static OpCode Callvirt => OpCodes.Callvirt;
        public static OpCode Stloc_0 => OpCodes.Stloc_0;
        public static OpCode Ldloc_0 => OpCodes.Ldloc_0;
        public static OpCode Ldc_I4_0 => OpCodes.Ldc_I4_0;
        public static OpCode Ldc_I4_1 => OpCodes.Ldc_I4_1;
        public static OpCode Brfalse_S => OpCodes.Brfalse_S;
        public static OpCode Brtrue_S => OpCodes.Brtrue_S;
        public static OpCode Br_S => OpCodes.Br_S;
    }

    public static class SymbolExtensions { public static MethodInfo GetMethodInfo(System.Linq.Expressions.Expression<Action> expression) => null; public static MethodInfo GetMethodInfo<T>(System.Linq.Expressions.Expression<Func<T>> expression) => null; }
    public static class GeneralExtensions { }
    public static class CollectionExtensions { public static void Do<T>(this IEnumerable<T> sequence, Action<T> action) { } public static void DoIf<T>(this IEnumerable<T> sequence, Func<T, bool> condition, Action<T> action) { } }

    [AttributeUsage(AttributeTargets.Class)]
    public class HarmonyPatchCategory : Attribute { public HarmonyPatchCategory(string category) { } }
    public static class Priority { public const int First = 0; public const int VeryHigh = 100; public const int High = 200; public const int HigherThanNormal = 300; public const int Normal = 400; public const int LowerThanNormal = 500; public const int Low = 600; public const int VeryLow = 700; public const int Last = 800; }

    public delegate IEnumerable<CodeInstruction> TranspilerDelegate(IEnumerable<CodeInstruction> instructions);
    public delegate IEnumerable<CodeInstruction> TranspilerWithILDelegate(IEnumerable<CodeInstruction> instructions, System.Reflection.Emit.ILGenerator il);

    public class CodeMatch
    {
        public CodeMatch(OpCode? opcode = null, object operand = null, string name = null) { }
        public CodeMatch(Func<CodeInstruction, bool> predicate, string name = null) { }
    }
    public class CodeMatcher
    {
        public CodeMatcher(IEnumerable<CodeInstruction> instructions, System.Reflection.Emit.ILGenerator generator = null) { }
        public CodeMatcher MatchStartForward(params CodeMatch[] matches) => this;
        public CodeMatcher MatchEndForward(params CodeMatch[] matches) => this;
        public CodeMatcher MatchStartBackwards(params CodeMatch[] matches) => this;
        public CodeMatcher Advance(int offset) => this;
        public CodeMatcher ThrowIfInvalid(string explanation) => this;
        public CodeMatcher Insert(params CodeInstruction[] instructions) => this;
        public CodeMatcher InsertAndAdvance(params CodeInstruction[] instructions) => this;
        public CodeMatcher RemoveInstructions(int count) => this;
        public CodeMatcher SetAndAdvance(OpCode opcode, object operand) => this;
        public CodeMatcher Set(OpCode opcode, object operand) => this;
        public CodeInstruction Instruction => null;
        public List<CodeInstruction> Instructions() => null;
        public List<CodeInstruction> InstructionEnumeration() => null;
        public bool IsValid => false;
        public bool IsInvalid => true;
        public int Pos => 0;
        public int Length => 0;
        public CodeMatcher Start() => this;
        public CodeMatcher End() => this;
        public CodeMatcher SearchForward(Func<CodeInstruction, bool> predicate) => this;
        public CodeMatcher SearchBackwards(Func<CodeInstruction, bool> predicate) => this;
        public object Operand => null;
        public OpCode Opcode => default;
        public List<System.Reflection.Emit.Label> Labels => null;
        public CodeMatcher SetOpcodeAndAdvance(OpCode opcode) => this;
        public CodeMatcher SetOperandAndAdvance(object operand) => this;
        public CodeMatcher CreateLabel(out System.Reflection.Emit.Label label) { label = default; return this; }
        public CodeMatcher AddLabels(IEnumerable<System.Reflection.Emit.Label> labels) => this;
        public CodeMatcher MatchForward(bool useEnd, params CodeMatch[] matches) => this;
    }
}
