namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Text;

    #endregion NameSpaces

    public static class ChoILGeneratorEx
	{
		public static void EmitLdind(this ILGenerator generator, Type type)
		{
			if (type.IsEnum)
				EmitLdind(generator, Enum.GetUnderlyingType(type));
			else if (type == typeof(Boolean) || type == typeof(SByte))
				generator.Emit(OpCodes.Ldind_I1);
			else if (type == typeof(Byte))
				generator.Emit(OpCodes.Ldind_U1);
			else if (type == typeof(Int16))
				generator.Emit(OpCodes.Ldind_I2);
			else if (type == typeof(UInt16) || type == typeof(Char))
				generator.Emit(OpCodes.Ldind_U2);
			else if (type == typeof(Int32))
				generator.Emit(OpCodes.Ldind_I4);
			else if (type == typeof(UInt32))
				generator.Emit(OpCodes.Ldind_U4);
			else if (type == typeof(Int64) || type == typeof(UInt64))
				generator.Emit(OpCodes.Ldind_I8);
			else if (type == typeof(Single))
				generator.Emit(OpCodes.Ldind_R4);
			else if (type == typeof(Double))
				generator.Emit(OpCodes.Ldind_R8);
			else if (type.IsValueType)
				generator.Emit(OpCodes.Ldobj, type);
			else if (type.IsPointer)
				generator.Emit(OpCodes.Ldind_I);
			else
				generator.Emit(OpCodes.Ldind_Ref);
		}

		public static void EmitStind(this ILGenerator generator, Type type)
		{
			if (type.IsEnum)
				EmitStind(generator, Enum.GetUnderlyingType(type));
			else if (type == typeof(Boolean) || type == typeof(SByte) || type == typeof(Byte))
				generator.Emit(OpCodes.Stind_I1);
			else if (type == typeof(Int16) || type == typeof(UInt16) || type == typeof(Char))
				generator.Emit(OpCodes.Stind_I2);
			else if (type == typeof(Int32) || type == typeof(UInt32))
				generator.Emit(OpCodes.Stind_I4);
			else if (type == typeof(Int64) || type == typeof(UInt64))
				generator.Emit(OpCodes.Stind_I8);
			else if (type == typeof(Single))
				generator.Emit(OpCodes.Stind_R4);
			else if (type == typeof(Double))
				generator.Emit(OpCodes.Stind_R8);
			else if (type.IsValueType)
				generator.Emit(OpCodes.Stobj, type);
			else if (type.IsPointer)
				generator.Emit(OpCodes.Stind_I);
			else
				generator.Emit(OpCodes.Stind_Ref);
		}

		public static void EmitLdarg(this ILGenerator generator, UInt16 index)
		{
			switch (index)
			{
				case 0:
					generator.Emit(OpCodes.Ldarg_0);
					break;
				case 1:
					generator.Emit(OpCodes.Ldarg_1);
					break;
				case 2:
					generator.Emit(OpCodes.Ldarg_2);
					break;
				case 3:
					generator.Emit(OpCodes.Ldarg_3);
					break;
				default:
					if (index > 255)
						generator.Emit(OpCodes.Ldarg, unchecked((Int16)index));
					else
						generator.Emit(OpCodes.Ldarg_S, (Byte)index);
					break;
			}
		}
	}
}
