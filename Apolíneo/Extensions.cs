using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Apolíneo
{
    public static class Extensions
    {
        public static String SplitSQL(this String texto, Char separator)
        {
            var array = texto.Split(separator);
            return JoinArraySQL(array);
        }

        public static String JoinArraySQL(this IList<String> texto)
        {
            var array = texto.Select(t => $"'{t}'").ToArray();
            return string.Join(",", array);
        }

        #region Controls

        public static void EnableDoubleClick(this Control control)
        {
            //habilita o doubleClick
            //http://stackoverflow.com/questions/13486245/winforms-how-to-call-a-double-click-event-on-a-button
            //https://msdn.microsoft.com/en-us/library/system.windows.forms.button.doubleclick.aspx
            MethodInfo methodSetStyle = control.GetType().GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            methodSetStyle.Invoke(control, new Object[] { ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true });
        }

        public static void EnableDoubleBuffered(this Control control, bool setting)
        {
            Type dgvType = control.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                  BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(control, setting, null);
        }

        #endregion Controls

        #region DataGridView

        public static void AjeitaDataGridView(this DataGridView dataGridView)
        {
            //para deixar o tamanho "certo e editavel" o tamanho da coluna // all cells bloqueia o usuario a nao editar
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            for (int i = 0; i < dataGridView.Columns.Count; i++)
            {
                int colw = dataGridView.Columns[i].Width;
                if (dataGridView.Columns[i].ValueType == typeof(Decimal))
                {
                    dataGridView.Columns[i].DefaultCellStyle.Format = "N2";
                }
                //
                dataGridView.Columns[i].Width = colw;
            }

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        public static void ColorirDataGridView(this DataGridView dataGridView)
        {
            dataGridView.RowsDefaultCellStyle.BackColor = System.Drawing.Color.LightCyan;
            dataGridView.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.Beige;

            dataGridView.EnableDoubleBuffered(true);
        }

        public static DataTable ConvertToDataTable(this DataGridView dataGridView)
        {
            DataTable table = new DataTable();

            //adiciona colunas
            for (int i = 0; i < dataGridView.Columns.Count; i++)
            {
                table.Columns.Add(dataGridView.Columns[i].Name, dataGridView.Columns[i].ValueType);
            }

            Object[] values = new Object[table.Columns.Count];
            for (int j = 0; j < dataGridView.Rows.Count; j++)
            {
                for (int k = 0; k < values.Length; k++)
                {
                    values[k] = dataGridView[k, j].Value;
                }
                table.Rows.Add(values);
            }

            return table;
        }

        
        #endregion DataGridView

        #region DataRow 

        
        #endregion DataRow

        #region IList

        public static DataTable ConvertToDatatable<T>(this IList source, Expression<Func<T, object>> columnPK)
        {
            String member = ((columnPK.Body as UnaryExpression).Operand as System.Linq.Expressions.MemberExpression).Member.Name;
            return ConvertToDatatable(source, member);
        }

        public static DataTable ConvertToDatatable(this IList source)
        {
            return ConvertToDatatable(source, null);
        }

        public static DataTable ConvertToDatatable(this IList source, String nameColumnPK)
        {
            if (source == null) throw new ArgumentNullException();
            var table = new DataTable();
            if (source.Count == 0) return table;

            Type itemType = source[0].GetType();
            table.TableName = itemType.Name;
            List<string> names = new List<string>();
            foreach (var prop in itemType.GetProperties())
            {
                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    names.Add(prop.Name);
                    Type propType = prop.PropertyType;
                    if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        propType = propType.GetGenericArguments()[0];
                    }
                    DataColumn column = new DataColumn(prop.Name, propType);
                    table.Columns.Add(column);
                    if (nameColumnPK != null && nameColumnPK == prop.Name)
                        table.PrimaryKey = new DataColumn[] { column };
                }
            }
            names.TrimExcess();

            var accessor = TypeAccessor.Create(itemType);
            object[] values = new object[names.Count];
            foreach (var row in source)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = accessor[row, names[i]];
                }
                table.Rows.Add(values);
            }

            return table;
        }

        /*private static bool IsNullable<T>(T obj)
        {
            if (obj == null) return true; // obvious
            Type type = typeof(T);
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }
        */
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        #endregion IList


    }
}

