using FastExpressionCompiler.LightExpression;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Extensions;

static class ReflectionExt {
	internal static Action<T, U> GetFieldSetter<T, U>(this Type type, string name) {
		var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
		return GetFieldSetter<T, U>(type, field);
	}

	internal static Action<T, U> GetFieldSetter<T, U>(this Type type, FieldInfo field) {
		var objExp = Expression.Parameter(type, "object");
		var valueExp = Expression.Parameter(typeof(U), "value");
		var convertedValueExp = Expression.Convert(valueExp, field.FieldType);

		var fieldExp = Expression.Field(objExp, field);
		Expression assignExp = Expression.Assign(fieldExp, convertedValueExp);
		if (!field.FieldType.IsClass) {
			assignExp = Expression.Convert(assignExp, typeof(object));
		}
		return Expression.Lambda<Action<T, U>>(assignExp, objExp, valueExp).CompileFast();
	}

	internal static Func<T, U> GetFieldGetter<T, U>(this Type type, string name) {
		var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
		return GetFieldGetter<T, U>(type, field);
	}

	internal static Func<T, U> GetFieldGetter<T, U>(this Type type, FieldInfo field) {
		var objExp = Expression.Parameter(type, "object");
		Expression fieldExp = Expression.Field(objExp, field);
		if (!field.FieldType.IsClass) {
			fieldExp = Expression.Convert(fieldExp, typeof(object));
		}
		return Expression.Lambda<Func<T, U>>(fieldExp, objExp).CompileFast();
	}

	internal static Action<T, U> GetPropertySetter<T, U>(this Type type, string name) {
		var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
		var objExp = Expression.Parameter(type, "object");
		var valueExp = Expression.Parameter(property.PropertyType, "value");
		var fieldExp = Expression.Property(objExp, property);
		var assignExp = Expression.Assign(fieldExp, valueExp);
		return Expression.Lambda<Action<T, U>>(assignExp, objExp, valueExp).CompileFast();
	}
}
