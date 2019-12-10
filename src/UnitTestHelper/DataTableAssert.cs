using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UnitTestHelper
{
    public class DataTableAssert
    {
        public static void AreEqual(DataTable expected, DataTable actual)
        {
            Assert.AreEqual(expected.Columns.Count, actual.Columns.Count,string.Format("Columns count different. Expected: {0} Actual: {1}",expected.Columns.Count,actual.Columns.Count));
            Assert.AreEqual(expected.Rows.Count, actual.Rows.Count, string.Format("Rows count different. Expected: {0} Actual: {1}", expected.Rows.Count, actual.Rows.Count));
            for (int colPos = 0; colPos < expected.Columns.Count; colPos++)
            {
                Assert.AreEqual(expected.Columns[colPos].ColumnName, actual.Columns[colPos].ColumnName, $"Column name different at position {colPos}. Expected: {expected.Columns[colPos].ColumnName} Actual: {actual.Columns[colPos].ColumnName}");
                Assert.AreEqual(expected.Columns[colPos].DataType, actual.Columns[colPos].DataType,$"Column datatype different at position {colPos}. Expected: {expected.Columns[colPos].DataType} Actual: {actual.Columns[colPos].DataType}");
                Assert.AreEqual(expected.Columns[colPos].AllowDBNull      , actual.Columns[colPos].AllowDBNull       , $"Column.AllowDBNull different at position {colPos}. Expected: {expected.Columns[colPos].AllowDBNull} Actual: {actual.Columns[colPos].AllowDBNull}");
                Assert.AreEqual(expected.Columns[colPos].AutoIncrement    , actual.Columns[colPos].AutoIncrement     , $"Column.AutoIncrement different at position {colPos}. Expected: {expected.Columns[colPos].AutoIncrement} Actual: {actual.Columns[colPos].AutoIncrement}");
                Assert.AreEqual(expected.Columns[colPos].AutoIncrementSeed, actual.Columns[colPos].AutoIncrementSeed , $"Column.AutoIncrementSeed different at position {colPos}. Expected: {expected.Columns[colPos].AutoIncrementSeed} Actual: {actual.Columns[colPos].AutoIncrementSeed}");
                Assert.AreEqual(expected.Columns[colPos].AutoIncrementStep, actual.Columns[colPos].AutoIncrementStep , $"Column.AutoIncrementStep different at position {colPos}. Expected: {expected.Columns[colPos].AutoIncrementStep} Actual: {actual.Columns[colPos].AutoIncrementStep}");
                Assert.AreEqual(expected.Columns[colPos].Caption          , actual.Columns[colPos].Caption           , $"Column.Caption different at position {colPos}. Expected: {expected.Columns[colPos].Caption} Actual: {actual.Columns[colPos].Caption}");
                Assert.AreEqual(expected.Columns[colPos].ColumnMapping    , actual.Columns[colPos].ColumnMapping     , $"Column.ColumnMapping different at position {colPos}. Expected: {expected.Columns[colPos].ColumnMapping} Actual: {actual.Columns[colPos].ColumnMapping}");
                Assert.AreEqual(expected.Columns[colPos].DateTimeMode     , actual.Columns[colPos].DateTimeMode      , $"Column.DateTimeMode different at position {colPos}. Expected: {expected.Columns[colPos].DateTimeMode} Actual: {actual.Columns[colPos].DateTimeMode}");
                Assert.AreEqual(expected.Columns[colPos].DefaultValue     , actual.Columns[colPos].DefaultValue      , $"Column.DefaultValue different at position {colPos}. Expected: {expected.Columns[colPos].DefaultValue} Actual: {actual.Columns[colPos].DefaultValue}");
                Assert.AreEqual(expected.Columns[colPos].Expression       , actual.Columns[colPos].Expression        , $"Column.Expression different at position {colPos}. Expected: {expected.Columns[colPos].Expression} Actual: {actual.Columns[colPos].Expression}");
                Assert.AreEqual(expected.Columns[colPos].ExtendedProperties, actual.Columns[colPos].ExtendedProperties, $"Column.ExtendedProperties different at position {colPos}. Expected: {expected.Columns[colPos].ExtendedProperties } Actual: {actual.Columns[colPos].ExtendedProperties}");
                Assert.AreEqual(expected.Columns[colPos].MaxLength        , actual.Columns[colPos].MaxLength         , $"Column.MaxLength different at position {colPos}. Expected: {expected.Columns[colPos].MaxLength} Actual: {actual.Columns[colPos].MaxLength}");
                Assert.AreEqual(expected.Columns[colPos].Namespace        , actual.Columns[colPos].Namespace         , $"Column.Namespace different at position {colPos}. Expected: {expected.Columns[colPos].Namespace} Actual: {actual.Columns[colPos].Namespace}");
                Assert.AreEqual(expected.Columns[colPos].Prefix           , actual.Columns[colPos].Prefix            , $"Column.Prefix different at position {colPos}. Expected: {expected.Columns[colPos].Prefix} Actual: {actual.Columns[colPos].Prefix}");
                Assert.AreEqual(expected.Columns[colPos].ReadOnly         , actual.Columns[colPos].ReadOnly          , $"Column.ReadOnly different at position {colPos}. Expected: {expected.Columns[colPos].ReadOnly} Actual: {actual.Columns[colPos].ReadOnly}");
                Assert.AreEqual(expected.Columns[colPos].Unique, actual.Columns[colPos].Unique, $"Column. different at position {colPos}. Expected: {expected.Columns[colPos].Unique} Actual: {actual.Columns[colPos].Unique}");


                //Assert.AreEqual(expected.Columns[colPos], actual.Columns[colPos], string.Format("Columns different at position {0}. Expected: {1} Actual: {2}",colPos, expected.Columns[colPos], actual.Columns[colPos]));

            //                Assert.AreEqual(expected.Columns[colPos]., actual.Columns[colPos].ColumnName);
            }
            for (int rowPos = 0; rowPos < expected.Rows.Count; rowPos++)
            {
                Assert.AreEqual(expected.Rows[rowPos].ItemArray.Length, actual.Rows[rowPos].ItemArray.Length, string.Format("Rows-ItemArray-Length are different at row {0}. Expected: {1} Actual: {2}",rowPos, expected.Rows[rowPos].ItemArray.Length, actual.Rows[rowPos].ItemArray.Length));
                for (int itemPos = 0; itemPos < expected.Rows[rowPos].ItemArray.Length; itemPos++)
                {
                    Assert.AreEqual(expected.Rows[rowPos].ItemArray[itemPos], actual.Rows[rowPos].ItemArray[itemPos], string.Format("Rows-ItemArray-Length are different at row {0} item {1}. Expected: {2} Actual: {3}", rowPos,itemPos, expected.Rows[rowPos].ItemArray[itemPos], actual.Rows[rowPos].ItemArray[itemPos]));
                }
            }
        }
    }
}
