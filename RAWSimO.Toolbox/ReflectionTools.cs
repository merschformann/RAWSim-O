using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// Contains some tools using reflection to get around repetitive implementations.
    /// </summary>
    public class ReflectionTools
    {
        /// <summary>
        /// Parses values from a string array and submits them to the given object.
        /// This method assumes that the values to set are the public fields of the given object and the order of the string values is consistent with the order of the public fields.
        /// Furthermore this method only parses some simple primitive types; currently supported: int, double, bool and string.
        /// </summary>
        /// <param name="receiver">The object which values will be set.</param>
        /// <param name="receiverType">The type of the receiving object.</param>
        /// <param name="fieldValues">The values in string form. The order and index has to be consistent with the one determined by reflecting on the type of the object.</param>
        /// <param name="formatter">The formatter to use for parsing the strings.</param>
        /// <param name="ignoreIndeces">Optionally some indeces that will be skipped and not parsed / set.</param>
        public static void ParseStringToFields(object receiver, Type receiverType, string[] fieldValues, IFormatProvider formatter, HashSet<int> ignoreIndeces = null)
        {
            FieldInfo[] fields = receiverType.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                // Just skip the indeces to ignore
                if (ignoreIndeces != null && ignoreIndeces.Contains(i))
                    continue;
                // Parse value according to field type
                if (fields[i].FieldType == typeof(int))
                    fields[i].SetValue(receiver, int.Parse(fieldValues[i], formatter));
                else if (fields[i].FieldType == typeof(double))
                    fields[i].SetValue(receiver, double.Parse(fieldValues[i], formatter));
                else if (fields[i].FieldType == typeof(bool))
                    fields[i].SetValue(receiver, bool.Parse(fieldValues[i]));
                else if (fields[i].FieldType == typeof(string))
                    fields[i].SetValue(receiver, fieldValues[i]);
                else if (fields[i].FieldType.IsEnum)
                    fields[i].SetValue(receiver, Enum.Parse(fields[i].FieldType, fieldValues[i]));
                else
                    throw new InvalidOperationException("Cannot set this field type! Either implement a case for this type or add the index to the ignore list.");
            }
        }
        /// <summary>
        /// Converts the object's public fields to strings using the given formatter.
        /// </summary>
        /// <param name="giver">The object which public fields shall be converted.</param>
        /// <param name="giverType">The type of the object.</param>
        /// <param name="formatter">The formatter to use for converting the objects.</param>
        /// <param name="doubleExportFormat">An export format to use for formatting double values.</param>
        /// <param name="ignoreIndeces">Optionally some indeces that will be skipped and not converted (null will be returned instead of a value).</param>
        /// <returns>The enumeration of string values with nulls as a replacement for ignored indeces.</returns>
        public static IEnumerable<string> ConvertFields(object giver, Type giverType, IFormatProvider formatter, string doubleExportFormat, HashSet<int> ignoreIndeces = null)
        {
            FieldInfo[] fields = giverType.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                // Just skip the indeces to ignore
                if (ignoreIndeces != null && ignoreIndeces.Contains(i))
                    yield return null;
                // Convert value to string
                if (fields[i].FieldType == typeof(int))
                    yield return ((int)fields[i].GetValue(giver)).ToString(formatter);
                else if (fields[i].FieldType == typeof(double))
                    yield return ((double)fields[i].GetValue(giver)).ToString(doubleExportFormat, formatter);
                else if (fields[i].FieldType == typeof(bool))
                    yield return ((bool)fields[i].GetValue(giver)).ToString();
                else if (fields[i].FieldType == typeof(string))
                    yield return (string)fields[i].GetValue(giver);
                else if (fields[i].FieldType.IsEnum)
                    yield return fields[i].GetValue(giver).ToString();
                else
                    throw new InvalidOperationException("Cannot set this field type! Either implement a case for this type or add the index to the ignore list.");
            }
        }
        /// <summary>
        /// Converts an objects public fields to an enumeration of string describing the fields, i.e. the names of the fields.
        /// </summary>
        /// <param name="giverType">The type of the object which public fields shall be converted to name strings.</param>
        /// <returns>An enumeration of name strings describing the fields.</returns>
        public static IEnumerable<string> ConvertFieldsToDescriptions(Type giverType)
        {
            FieldInfo[] fields = giverType.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                // Simply return the field's name
                yield return fields[i].Name;
            }
        }
    }
}
