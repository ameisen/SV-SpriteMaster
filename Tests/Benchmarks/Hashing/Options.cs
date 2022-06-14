using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using SpriteMaster.Extensions;
using System.Reflection;

namespace Hashing;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
internal sealed class OptionAttribute : Attribute {
	internal readonly string Description;
	internal readonly string LongOpt;
	internal readonly char? ShortOpt = null;

	internal OptionAttribute(string longOpt, string description) {
		Description = description;
		LongOpt = longOpt;
	}

	internal OptionAttribute(string longOpt, char shortOpt, string description) {
		Description = description;
		LongOpt = longOpt;
		ShortOpt = shortOpt;
	}
}

internal enum GCType {
	Workstation = 0,
	Server
}

internal abstract class OptionsBuilder {
	[Option("min", "Minimum Range Value")]
	internal long Min { get; set; } = 0;

	[Option("max", "Maximum Range Value")]
	internal long Max { get; set; } = 0x10000;

	[Option("dictionary", "Dictionary for dictionary set")]
	internal string Dictionary { get; set; } = @"D:\Stardew\SpriteMaster\Tests\Benchmarks\Hashing\bin\dictionary.zip";

	internal HashSet<string> Set { get; } = new(StringComparer.InvariantCultureIgnoreCase);

	internal HashSet<string> Runners { get; } = new(StringComparer.InvariantCultureIgnoreCase);

	[Option("diag-memory", "Memory/Allocation Diagnostics")]
	internal bool DiagnoseMemory { get; set; } = false;

	[Option("diag-inlining", "Inlining Diagnostics")]
	internal bool DiagnoseInlining { get; init; } = false;

	[Option("diag-tailcall", "Tail Call Diagnostics")]
	internal bool DiagnoseTailCall { get; init; } = false;

	[Option("diag-etw", "ETW Diagnostics")]
	internal bool DiagnoseEtw { get; init; } = false;

	[Option("cold", "Test Cold Start")]
	internal bool Cold { get; init; } = false;

	[Option("validate", "Perform Validation")]
	internal bool DoValidate { get; init; } = false;

	internal HashSet<GCType> GCTypes { get; } = new();

	internal HashSet<Runtime> Runtimes { get; } = new();

	internal void Validate(HashSet<string> validSets) {
		if (Set.Count == 0) {
			Set.AddRange(validSets);
		}
	}
}

internal sealed class Options : OptionsBuilder {
	internal static Options Default => new();

	private OptionsBuilder Builder => this;

	internal new long Min => Builder.Min;

	internal new long Max => Builder.Max;

	internal new bool DiagnoseMemory => Builder.DiagnoseMemory;

	internal new bool DiagnoseInlining => Builder.DiagnoseInlining;

	internal new bool DiagnoseTailCall => Builder.DiagnoseTailCall;

	internal new bool DiagnoseEtw => Builder.DiagnoseEtw;

	private Options() { }

	private static readonly HashSet<string> ValidSets = new(
		typeof(Program).Assembly.GetTypes()
			.Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Benchmarks.BenchmarkBase)))
			.Select(type => type.Name),
		StringComparer.InvariantCultureIgnoreCase
	);

	private static readonly HashSet<string> ValidRunners = new(
		typeof(Benchmarks.BenchmarkBaseImpl<Benchmarks.DataSet<string>, string>).GetMethods()
			.Where(method => method.HasAttribute<BenchmarkAttribute>())
			.Select(method => method.Name),
		StringComparer.InvariantCultureIgnoreCase
	);

	private static readonly MemberInfo[] OptionFields = typeof(OptionsBuilder).
		GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).
		Where(member => member.HasAttribute<OptionAttribute>()).ToArray();

	private static readonly Dictionary<string, MemberInfo> OptionFieldsLongMap = new(
		OptionFields.Select(
			field => {
				var attr = field.GetCustomAttribute<OptionAttribute>()!;
				return new KeyValuePair<string, MemberInfo>(attr.LongOpt, field);
			}
		),
		StringComparer.InvariantCultureIgnoreCase
	);

	private static readonly Dictionary<char, MemberInfo> OptionFieldsShortMap = new(
		OptionFields
			.Where(
				field => {
					var attr = field.GetCustomAttribute<OptionAttribute>()!;
					return attr.ShortOpt.HasValue;
				}
			)
			.Select(
				field => {
					var attr = field.GetCustomAttribute<OptionAttribute>()!;
					return new KeyValuePair<char, MemberInfo>(attr.ShortOpt.Value, field);
				}
			)
	);

	private static readonly Runtime CoreRuntime7 = CoreRuntime.CreateForNewVersion("net7.0", ".NET 7.0");

	internal static Options From(string[] args) {
		OptionsBuilder result = new Options();

		List<string> fatal = new();

		void FatalError(string message) {
			Console.Error.WriteLine(message);
			fatal.Add(message);
		}

		bool onlyList = false;

		static bool RemoveFromStart(ref string str, string comparand) {
			if (str.StartsWith(comparand)) {
				str = str[comparand.Length..];
				return true;
			}

			return false;
		}

		static bool RemoveFromEnd(ref string str, string comparand) {
			if (str.EndsWith(comparand)) {
				str = str[..^comparand.Length];
				return true;
			}

			return false;
		}

		void ParseFlag(string flag, bool isShortFlag) {
			bool? state = null;

			if (RemoveFromStart(ref flag, "no-")) {
				state = false;
			}
			if (RemoveFromEnd(ref flag, "-")) {
				state = false;
			}

			string? arg = null;

			uint equals = (uint)flag.IndexOf('=');
			uint colon = (uint)flag.IndexOf(':');
			if ((int)equals != -1 || (int)colon != -1) {
				uint offset = Math.Min(equals, colon);

				arg = flag[((int)offset + 1)..];
				flag = flag[..(int)offset];
			}

			switch (flag) {
				case "list-runners": {
					Console.WriteLine("Valid Runners:");
					foreach (var runner in ValidRunners) {
						Console.WriteLine($"  {runner}");
					}

					onlyList = true;
					break;
				}
				case "list-sets": {
					Console.WriteLine("Valid Sets:");
					foreach (var set in ValidSets) {
						Console.WriteLine($"  {set}");
					}

					onlyList = true;
					break;
				}
				case "run":
				case "r" when isShortFlag: {
					if (arg is null) {
						FatalError($"'run' requires an argument");
						break;
					}

					var localRunners = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);
					result.Runners.AddRange(localRunners);

					foreach (var runner in localRunners) {
						if (!ValidRunners.Contains(runner)) {
							FatalError($"Unknown runner: '{runner}'");
							break;
						}
					}

					break;
				}
				default:
					MemberInfo? member;

					if (isShortFlag) {
						if (OptionFieldsShortMap.TryGetValue(flag[0], out member)) {

						}
					}
					else {
						if (OptionFieldsLongMap.TryGetValue(flag, out member)) {

						}
					}

					switch (flag.ToLowerInvariant()) {
						case "gc":
							if (arg is null) {
								FatalError("No GC specified");
								break;
							}
							switch (arg.ToLowerInvariant()) {
								case "default":
								case "workstation":
									result.GCTypes.Add(GCType.Workstation);
									break;
								case "server":
									result.GCTypes.Add(GCType.Server);
									break;
								default:
									FatalError($"Unknown GC: {arg}");
									break;
							}
							return;
						case "runtime":
							if (arg is null) {
								FatalError("No runtime specified");
								break;
							}

							var args = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);
							foreach (var rt in args) {
								switch (rt.ToLowerInvariant()) {
									case "3.1":
										result.Runtimes.Add(CoreRuntime.Core31);
										break;
									case "5":
									case "5.1":
										result.Runtimes.Add(CoreRuntime.Core50);
										break;
									case "6":
									case "6.0":
										result.Runtimes.Add(CoreRuntime.Core60);
										break;
									case "7":
									case "7.0":
										result.Runtimes.Add(CoreRuntime7);
										break;
									default:
										FatalError($"Unknown Runtime: {rt}");
										break;
								}
							}

							return;
					}

					if (member is not null) {
						switch (member) {
							case FieldInfo field: {
								object? par = null;
								var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
								if (type == typeof(bool)) {
									par = arg is null ? state : bool.Parse(arg);
									par ??= true;
								}
								else if (type == typeof(long)) {
									par = arg is null ? null : long.Parse(arg);
								}
								if (par is not null) {
									field.SetValue(result, par);
								}

								break;
							}
							case PropertyInfo property: {
								object? par = null;
								var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
								if (type == typeof(bool)) {
									par = arg is null ? state : bool.Parse(arg);
									par ??= true;
									}
								else if (type == typeof(long)) { 
									par = arg is null ? null : long.Parse(arg);
								}
								if (par is not null) {
									property.SetValue(result, par);
								}

								break;
							}
							case MethodInfo method:
								method.Invoke(result, new object?[] { (object?)state ?? arg });
								break;
							default:
								throw new NotImplementedException();
						} ;
					}
					else {
						FatalError($"Unknown flag: '{flag}'");
						break;
					}

					break;
			}
		}

		bool isInArgs = false;
		foreach (var arg in args) {
			if (arg.Length == 0) {
				continue;
			}

			switch (arg) {
				case "--" when !isInArgs: {
					isInArgs = true;
					break;
				}
				case var flag when !isInArgs && (arg[0] == '/' || arg[0] == '-'): {
					bool shortFlag = false;

					if (arg.StartsWith("--")) {
						flag = flag[2..];
					}
					else {
						shortFlag = arg[0] == '-';
						flag = flag[1..];
					}

					ParseFlag(flag, isShortFlag: shortFlag);
					break;
				}
				default: {
					if (!ValidSets.Contains(arg)) {
						FatalError($"Unknown set specified: '{arg}'");
						break;
					}

					if (!result.Set.Add(arg)) {
						Console.Error.WriteLine($"Duplicate test set specified: '{arg}'");
					}
					break;
				}
			}
		}

		if (fatal.Count != 0) {
			Console.Error.WriteLine($"There were {fatal.Count} fatal errors in configuration.");
			Environment.Exit(-fatal.Count);
		}

		if (onlyList) {
			Environment.Exit(0);
		}

		result.Validate(ValidSets);
		return (result as Options)!;
	}
}
