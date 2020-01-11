using Harmony;
using Microsoft.Xna.Framework;
using SpriteMaster.Extensions;
using SpriteMaster.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpriteMaster.Harmonize {
	using MethodEnumerable = IEnumerable<MethodInfo>;

	internal static class Harmonize {
		private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		public enum PriorityLevel : int {
			Last = int.MinValue,
			Lowest = Harmony.Priority.Last,
			VeryLow = Harmony.Priority.VeryLow,
			Low = Harmony.Priority.Low,
			BelowAverage = Harmony.Priority.LowerThanNormal,
			Average = Harmony.Priority.Normal,
			AboveAverage = Harmony.Priority.HigherThanNormal,
			High = Harmony.Priority.High,
			VeryHigh = Harmony.Priority.VeryHigh,
			Highest = Harmony.Priority.First,
			First = int.MaxValue
		}

		public enum AffixType {
			Prefix,
			Postfix,
			Transpile
		}

		public enum GenericType {
			None,
			Struct,
			Unmanaged,
			Class
		}

		internal static readonly Type[] UnmanagedTypes = new[] {
			typeof(char),
			typeof(byte),
			typeof(sbyte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(float),
			typeof(double)
		};

		internal static readonly Type[] StructTypes = new[] {
			typeof(Vector2),
			typeof(Vector3),
			typeof(Vector4),
			typeof(Color),
			typeof(System.Drawing.Color)
		}.Concat(UnmanagedTypes).ToArray();

		private static Type[] GetGenericTypeArray(GenericType genericType) {
			switch (genericType) {
				case GenericType.None: return Arrays<Type>.Empty;
				case GenericType.Struct: return StructTypes;
				case GenericType.Unmanaged: return UnmanagedTypes;
				default: throw new ArgumentOutOfRangeException(nameof(genericType));
			}
		}

		public static void ApplyPatches(this HarmonyInstance @this) {
			Contract.AssertNotNull(@this);
			var assembly = typeof(Harmonize).Assembly;
			foreach (var type in assembly.GetTypes()) {
				foreach (var method in type.GetMethods(StaticFlags)) {
					try {
						var attributes = method.GetCustomAttributes().OfType<HarmonizeAttribute>();
						if (attributes == null || !attributes.Any())
							continue;

						try {
							foreach (var attribute in attributes) {
								if (!attribute.CheckPlatform())
									continue;

								Debug.TraceLn($"Patching Method {method.GetFullName()}");

								var instanceType = attribute.Type;
								if (instanceType == null) {
									var instancePar = method.GetParameters().Where(p => p.Name == "__instance");
									Contract.AssertTrue(instancePar.Count() != 0, $"Type not specified for method {method.GetFullName()}, but no __instance argument present");
									instanceType = instancePar.First().ParameterType.RemoveRef();
								}

								switch (attribute.Generic) {
									case GenericType.None:
										Patch(
											@this,
											instanceType,
											attribute.Method,
											pre: (attribute.Affix == AffixType.Prefix) ? method : null,
											post: (attribute.Affix == AffixType.Postfix) ? method : null,
											trans: (attribute.Affix == AffixType.Transpile) ? method : null,
											instanceMethod: attribute.Instance
										);
										break;
									case GenericType.Struct:
									case GenericType.Unmanaged:
										foreach (var genericType in GetGenericTypeArray(attribute.Generic)) {
											Debug.TraceLn($"\tGeneric Type: {genericType.FullName}");
											Patch(
												@this,
												instanceType,
												genericType,
												attribute.Method,
												pre: (attribute.Affix == AffixType.Prefix) ? method : null,
												post: (attribute.Affix == AffixType.Postfix) ? method : null,
												trans: (attribute.Affix == AffixType.Transpile) ? method : null,
												instanceMethod: attribute.Instance
											);
										}
										break;
									default:
										throw new NotImplementedException("Non-struct Generic Harmony Types unimplemented");
								}
							}
						}
						catch (Exception ex) {
							Debug.TraceLn($"Exception Patching Method {method.GetFullName()}");
							ex.PrintError();
						}
					}
					catch (Exception ex) {
						Debug.TraceLn($"Exception Patching Method {method.GetFullName()}");
						ex.PrintError();
						continue;
					}
				}
			}
		}

		private static Type[] GetArguments (this MethodInfo method) {
			var filteredParameters = method.GetParameters().Where(t => !t.Name.StartsWith("__") || t.Name.StartsWith("__unnamed"));
			return filteredParameters.Select(p => p.ParameterType).ToArray();
		}

		public static MethodEnumerable GetMethods (this Type type, string name, BindingFlags bindingFlags) {
			return type.GetMethods(bindingFlags).Where(t => t.Name == name);
		}

		public static MethodEnumerable GetStaticMethods (this Type type, string name) {
			return type.GetMethods(name, StaticFlags);
		}

		public static MethodInfo GetStaticMethod (this Type type, string name) {
			return type.GetMethod(name, StaticFlags);
		}

		public static MethodEnumerable GetInstanceMethods (this Type type, string name) {
			return type.GetMethods(name, InstanceFlags);
		}

		public static MethodInfo GetInstanceMethod (this Type type, string name) {
			return type.GetMethod(name, InstanceFlags);
		}

		public static MethodEnumerable GetMethods<T> (string name, BindingFlags bindingFlags) {
			return typeof(T).GetMethods(name, bindingFlags);
		}

		public static MethodEnumerable GetStaticMethods<T> (string name) {
			return typeof(T).GetStaticMethods(name);
		}

		public static MethodEnumerable GetInstanceMethods<T> (string name) {
			return typeof(T).GetInstanceMethods(name);
		}

		private static MethodBase GetPatchMethod (Type type, string name, MethodInfo method, bool instance, bool transpile = false) {
			bool constructor = (name == ".ctor");
			var flags = instance ? InstanceFlags : StaticFlags;

			if (transpile) {
				if (constructor) {
					// TODO fix me
					return type.GetConstructor(Type.EmptyTypes);
				}
				else {
					return type.GetInstanceMethod(name);
				}
			}

			var methodParameters = method.GetArguments();
			var typeMethod = constructor ? (MethodBase)type.GetConstructor(methodParameters) : (MethodBase)type.GetMethod(name, methodParameters);
			if (typeMethod == null) {
				MethodBase[] typeMethods;
				if (constructor) {
					typeMethods = type.GetConstructors(flags);
				}
				else {
					var methods = type.GetMethods(name, flags);
					typeMethods = new MethodBase[methods.Count()];
					foreach (int i in 0..typeMethods.Length) {
						typeMethods[i] = methods.ElementAt(i);
					}

				}
				foreach (var testMethod in typeMethods) {
					// Compare the parameters. Ignore references.
					var testParameters = testMethod.GetParameters();
					if (testParameters.Length != methodParameters.Length) {
						continue;
					}

					bool found = true;
					foreach (int i in 0..testParameters.Length) {
						var testParameter = testParameters[i].ParameterType.RemoveRef();
						var testParameterRef = testParameter.AddRef();
						var testBaseParameter = testParameter.IsArray ? testParameter.GetElementType() : testParameter;
						var methodParameter = methodParameters[i].RemoveRef();
						var methodParameterRef = methodParameter.AddRef();
						var baseParameter = methodParameter.IsArray ? methodParameter.GetElementType() : methodParameter;
						if (
							!(testParameter.IsPointer && methodParameter.IsPointer) &&
							!testParameterRef.Equals(methodParameterRef) &&
							!(testBaseParameter.IsGenericParameter && baseParameter.IsGenericParameter) &&
							!methodParameter.Equals(typeof(object)) && !(testParameter.IsArray && methodParameter.IsArray && baseParameter.Equals(typeof(object)))) {
							found = false;
							break;
						}
					}
					if (found) {
						typeMethod = testMethod;
						break;
					}
				}

				if (typeMethod == null) {
					Debug.ErrorLn($"Failed to patch {type.Name}.{name}");
					return null;
				}
			}
			return typeMethod;
		}

		internal static int GetPriority (MethodInfo method, int defaultPriority) {
			try {
				if (method.GetCustomAttribute<HarmonyPriority>() is var priorityAttribute && priorityAttribute != null) {
					return priorityAttribute.info.prioritiy;
				}
			}
			catch { }
			return defaultPriority;
		}

		private static void Patch(this HarmonyInstance instance, Type type, string name, MethodInfo pre = null, MethodInfo post = null, MethodInfo trans = null, int priority = Priority.Last, bool instanceMethod = true) {
			var referenceMethod = pre ?? post;
			if (referenceMethod != null) {
				var typeMethod = GetPatchMethod(type, name, referenceMethod, instance: instanceMethod) ?? throw new ArgumentException($"Method '{name}' in type '{type.FullName}' could not be found");
				instance.Patch(
					typeMethod,
					(pre == null) ? null : new HarmonyMethod(pre) { prioritiy = GetPriority(pre, priority) },
					(post == null) ? null : new HarmonyMethod(post) { prioritiy = GetPriority(post, priority) },
					null
				);
			}
			if (trans != null) {
				var typeMethod = GetPatchMethod(type, name, referenceMethod, transpile: true, instance: instanceMethod) ?? throw new ArgumentException($"Method '{name}' in type '{type.FullName}' could not be found");
				instance.Patch(
					typeMethod,
					null,
					null,
					new HarmonyMethod(trans)// { prioritiy = GetPriority(trans, priority) }
				);
			}
		}

		public static void Patch<T> (this HarmonyInstance instance, string name, MethodInfo pre = null, MethodInfo post = null, MethodInfo trans = null, int priority = Priority.Last) {
			Patch(instance, typeof(T), name, pre, post, trans, priority);
		}

		public static void Patch<T> (this HarmonyInstance instance, string name, MethodEnumerable pre = default, MethodEnumerable post = default, MethodEnumerable trans = default, int priority = Priority.Last) {
			if (pre != null)
				foreach (var method in pre) {
					Patch<T>(instance, name, pre: method, post: null, trans: null, priority);
				}
			if (post != null)
				foreach (var method in post) {
					Patch<T>(instance, name, pre: null, post: method, trans: null, priority);
				}
			if (trans != null)
				foreach (var method in trans) {
					Patch<T>(instance, name, pre: null, post: null, trans: method, priority);
				}
		}

		public static void Patch (this HarmonyInstance instance, Type type, Type genericType, string name, MethodInfo pre = null, MethodInfo post = null, MethodInfo trans = null, int priority = Priority.Last, bool instanceMethod = true) {
			var referenceMethod = pre ?? post;
			if (referenceMethod != null) {
				var typeMethod = GetPatchMethod(type, name, referenceMethod, instance: instanceMethod) ?? throw new ArgumentException($"Method '{name}' in type '{type.FullName}' could not be found");
				var typeMethodInfo = (MethodInfo)typeMethod;
				instance.Patch(
					typeMethodInfo.MakeGenericMethod(genericType),
					(pre == null) ? null : new HarmonyMethod(pre.MakeGenericMethod(genericType)) { prioritiy = GetPriority(pre, priority) },
					(post == null) ? null : new HarmonyMethod(post.MakeGenericMethod(genericType)) { prioritiy = GetPriority(post, priority) },
					null
				);
			}
			if (trans != null) {
				var typeMethod = GetPatchMethod(type, name, referenceMethod, transpile: true, instance: instanceMethod) ?? throw new ArgumentException($"Method '{name}' in type '{type.FullName}' could not be found");
				var typeMethodInfo = (MethodInfo)typeMethod;
				instance.Patch(
					typeMethodInfo.MakeGenericMethod(genericType),
					null,
					null,
					new HarmonyMethod(trans.MakeGenericMethod(genericType)) { prioritiy = GetPriority(trans, priority) }
				);
			}
		}

		public static void Patch<T> (this HarmonyInstance instance, Type genericType, string name, MethodInfo pre = null, MethodInfo post = null, MethodInfo trans = null, int priority = Priority.Last) {
			Patch(instance, typeof(T), genericType, name, pre, post, trans, priority);
		}

		public static void Patch<T, U> (this HarmonyInstance instance, string name, MethodInfo pre = null, MethodInfo post = null, MethodInfo trans = null, int priority = Priority.Last) where U : struct {
			Patch(instance, typeof(T), typeof(U), name, pre, post, trans, priority);
		}

		public static void Patch<T, U> (this HarmonyInstance instance, string name, MethodEnumerable pre = default, MethodEnumerable post = default, MethodEnumerable trans = default, int priority = Priority.Last) where U : struct {
			if (pre != null)
				foreach (var method in pre) {
					Patch<T, U>(instance, name, pre: method, post: null, trans: null, priority);
				}
			if (post != null)
				foreach (var method in post) {
					Patch<T, U>(instance, name, pre: null, post: method, trans: null, priority);
				}
			if (trans != null)
				foreach (var method in post) {
					Patch<T, U>(instance, name, pre: null, post: null, trans: method, priority);
				}
		}
	}
}
