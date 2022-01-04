using FastExpressionCompiler.LightExpression;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Extensions;

static partial class ReflectionExt {
	private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;

	internal static Action<T, U> GetFieldSetter<T, U>(this Type type, string name) => GetFieldSetter<T, U>(type, type.GetField(name, InstanceFlags));

	internal static Action<T, U> GetFieldSetter<T, U>(this Type type, FieldInfo field) {
		var objExp = Expression.Parameter(type, "object");
		var valueExp = Expression.Parameter(typeof(U), "value");
		var convertedValueExp = Expression.Convert(valueExp, field.FieldType);

		var memberExp = Expression.Field(objExp, field);
		Expression assignExp = Expression.Assign(memberExp, convertedValueExp);
		if (!field.FieldType.IsClass) {
			assignExp = Expression.Convert(assignExp, typeof(object));
		}
		return Expression.Lambda<Action<T, U>>(assignExp, objExp, valueExp).CompileFast();
	}

	internal static Func<T, U> GetFieldGetter<T, U>(this Type type, string name) => GetFieldGetter<T, U>(type, type.GetField(name, InstanceFlags));

	internal static Func<T, U> GetFieldGetter<T, U>(this Type type, FieldInfo field) {
		var objExp = Expression.Parameter(type, "object");
		Expression memberExp = Expression.Field(objExp, field);
		if (!field.FieldType.IsClass) {
			memberExp = Expression.Convert(memberExp, typeof(object));
		}
		return Expression.Lambda<Func<T, U>>(memberExp, objExp).CompileFast();
	}

	internal static Action<T, U> GetPropertySetter<T, U>(this Type type, string name) => GetPropertySetter<T, U>(type, type.GetProperty(name, InstanceFlags));

	internal static Action<T, U> GetPropertySetter<T, U>(this Type type, PropertyInfo property) {
		var objExp = Expression.Parameter(type, "object");
		var valueExp = Expression.Parameter(property.PropertyType, "value");
		var memberExp = Expression.Property(objExp, property);
		var assignExp = Expression.Assign(memberExp, valueExp);
		return Expression.Lambda<Action<T, U>>(assignExp, objExp, valueExp).CompileFast();
	}

	internal static Func<T, U> GetPropertyGetter<T, U>(this Type type, string name) => GetPropertyGetter<T, U>(type, type.GetProperty(name, InstanceFlags));

	internal static Func<T, U> GetPropertyGetter<T, U>(this Type type, PropertyInfo property) {
		var objExp = Expression.Parameter(type, "object");
		Expression memberExp = Expression.Property(objExp, property);
		if (!property.PropertyType.IsClass) {
			memberExp = Expression.Convert(memberExp, typeof(object));
		}
		return Expression.Lambda<Func<T, U>>(memberExp, objExp).CompileFast();
	}

	internal static Action<T, U> GetMemberSetter<T, U>(this Type type, string name) => GetMemberSetter<T, U>(type, type.GetPropertyOrField(name, InstanceFlags));

	internal static Action<T, U> GetMemberSetter<T, U>(this Type type, MemberInfo member) {
		switch (member) {
			case FieldInfo field:
				return GetFieldSetter<T, U>(type, field);
			case PropertyInfo property:
				return GetPropertySetter<T, U>(type, property);
			default:
				return null;
		}
	}

	internal static Func<T, U> GetMemberGetter<T, U>(this Type type, string name) => GetMemberGetter<T, U>(type, type.GetPropertyOrField(name, InstanceFlags));

	internal static Func<T, U> GetMemberGetter<T, U>(this Type type, MemberInfo member) {
		switch (member) {
			case FieldInfo field:
				return GetFieldGetter<T, U>(type, field);
			case PropertyInfo property:
				return GetPropertyGetter<T, U>(type, property);
			default:
				return null;
		}
	}
}
