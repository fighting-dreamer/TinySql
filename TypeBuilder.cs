﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;

namespace TinySql
{
    public class TypeCache
    {
        private TypeCache()
        {

        }
        private static TypeCache instance = null;
        public static TypeCache Default
        {
            get
            {
                if (instance == null)
                {
                    instance = new TypeCache();
                }
                return instance;
            }
        }

        //private static ConcurrentDictionary<Type, SqlBuilder> _Select = null;




    }


    public static class TypeBuilder
    {
        public static SqlBuilder Select(Type ObjectType, string TableName = null, string[] Properties = null, string[] ExcludeProperties = null, int? Top = null, bool Distinct = false)
        {
            SqlBuilder builder = SqlBuilder.Select(Top, Distinct);
            Table BaseTable = null;
            if (string.IsNullOrEmpty(TableName))
            {
                BaseTable = builder.From(ObjectType.Name);
            }
            else
            {
                BaseTable = builder.From(TableName);
            }
            if (Properties == null)
            {
                Properties = ObjectType.GetProperties().Select(x => x.Name).Union(ObjectType.GetFields().Select(x => x.Name)).ToArray();
            }
            if (ExcludeProperties == null)
            {
                ExcludeProperties = new string[0];
            }
            foreach (string Name in Properties.Except(ExcludeProperties))
            {
                if (Name.Equals("*"))
                {
                    BaseTable.AllColumns(false);
                    return builder;
                }
                BaseTable.Column(Name);
            }
            return builder;
        }

        public static SqlBuilder Update<T>(T instance, string TableName = null, string Schema = null, string[] Properties = null, string[] ExcludeProperties = null, bool OutputPrimaryKey = false)
        {
            UpdateTable table = SqlBuilder.Update()
                .Table(TableName ?? instance.GetType().Name,Schema);

            Metadata.MetadataTable mt = SqlBuilder.DefaultMetadata.FindTable(TableName ?? instance.GetType().Name);

            if (Properties == null)
            {
                Properties = instance.GetType().GetProperties().Select(x => x.Name).ToArray();
            }
            if (ExcludeProperties != null)
            {
                Properties = Properties.Except(ExcludeProperties).ToArray();
            }

            foreach (Metadata.MetadataColumn col in mt.Columns.Values)
            {
                if (Properties.Contains(col.Name) && !col.IsIdentity && !col.IsReadOnly)
                {
                    PropertyInfo prop = instance.GetType().GetProperty(col.Name);
                    if (prop.CanRead && prop.CanWrite)
                    {
                        table.Set(col.Name, prop.GetValue(instance), col.SqlDataType,prop.PropertyType);
                    }
                }
            }
            List<object> pk = new List<object>();
            mt.PrimaryKey.Columns.ForEach((col) =>
            {
                PropertyInfo prop = instance.GetType().GetProperty(col.Name);
                pk.Add(prop.GetValue(instance));
            });
            table.WithMetadata().WherePrimaryKey(pk.ToArray());
            if (OutputPrimaryKey)
            {
                return table.Output().PrimaryKey().Builder();
            }
            else
            {
                return table.Builder();
            }
            
        }


        public static SqlBuilder Insert<T>(T instance, string TableName = null, string[] Properties = null, string[] ExcludeProperties = null)
        {
            InsertIntoTable table = SqlBuilder.Insert()
                .Into(TableName ?? instance.GetType().Name);

            Metadata.MetadataTable mt = SqlBuilder.DefaultMetadata.FindTable(TableName ?? instance.GetType().Name);


            if (Properties == null)
            {
                Properties = instance.GetType().GetProperties().Select(x => x.Name).ToArray();
            }
            if (ExcludeProperties != null)
            {
                Properties = Properties.Except(ExcludeProperties).ToArray();
            }
            foreach (Metadata.MetadataColumn col in mt.Columns.Values)
            {
                if (Properties.Contains(col.Name) && !col.IsIdentity && !col.IsReadOnly)
                {
                    PropertyInfo prop = instance.GetType().GetProperty(col.Name);
                    if (prop.CanRead && prop.CanWrite)
                    {
                        table.Value(prop.Name, prop.GetValue(instance));
                    }
                }
            }


            return table.Output().PrimaryKey().Builder();
        }

        public static SqlBuilder Update<TModel, TProperty>(this TableHelper<TModel> helper, TModel Instance, Expression<Func<TModel, TProperty>> prop)
        {
            return helper.table.Builder;
        }

        public static void UpdateEx<TModel, TProperty>(this ModelHelper<TModel> helper, Expression<Func<TModel, TProperty>> prop)
        {
            TProperty t = prop.Compile().Invoke(helper.Model);


        }

        public class ModelHelper<TModel>
        {
            public TModel Model { get; set; }
            public ModelHelper(TModel model)
            {
                this.Model = model;
            }
        }





        public static SqlBuilder Select<T>(string TableName = null, string[] Properties = null, string[] ExcludeProperties = null, int? Top = null, bool Distinct = false)
        {

            return Select(typeof(T), TableName, Properties, ExcludeProperties, Top, Distinct);
        }

        public static T PopulateObject<T>(T instance, DataTable dt, DataRow row, bool AllowPrivateProperties, bool EnforceTypesafety)
        {
            foreach (DataColumn col in dt.Columns)
            {
                BindingFlags flag = BindingFlags.Public;
                if (AllowPrivateProperties)
                {
                    flag = flag | BindingFlags.NonPublic;
                }
                PropertyInfo prop = instance.GetType().GetProperty(col.ColumnName, BindingFlags.Instance | flag);
                FieldInfo field = null;
                if (prop == null)
                {
                    field = instance.GetType().GetField(col.ColumnName, BindingFlags.Instance | flag);
                    if (field != null)
                    {
                        if (field.FieldType == typeof(XmlDocument) && !row.IsNull(col))
                        {
                            if (col.DataType == typeof(string))
                            {
                                XmlDocument xml = new XmlDocument();
                                xml.LoadXml((string)row[col]);
                                field.SetValue(instance, xml);
                            }
                            else if (col.DataType == typeof(XmlDocument))
                            {
                                field.SetValue(instance, (XmlDocument)row[col]);
                            }
                        }
                        else if (!EnforceTypesafety || field.FieldType == col.DataType)
                        {
                            if (row.IsNull(col))
                            {
                                if (!field.FieldType.IsValueType || Nullable.GetUnderlyingType(field.FieldType) != null)
                                {
                                    field.SetValue(instance, null);
                                }
                            }
                            else
                            {
                                field.SetValue(instance, row[col.ColumnName]);
                            }
                        }
                    }
                    else
                    {
                        continue;
                    }

                }
                else
                {
                    if (prop.CanWrite)
                    {
                        if (prop.PropertyType == typeof(XmlDocument) && !row.IsNull(col))
                        {
                            if (col.DataType == typeof(string))
                            {
                                XmlDocument xml = new XmlDocument();
                                xml.LoadXml((string)row[col]);
                                prop.SetValue(instance, xml, null);
                            }
                            else if (col.DataType == typeof(XmlDocument))
                            {
                                prop.SetValue(instance, (XmlDocument)row[col], null);
                            }
                        }
                        else if (!EnforceTypesafety || prop.PropertyType == col.DataType)
                        {
                            if (row.IsNull(col))
                            {
                                if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null)
                                {
                                    prop.SetValue(instance, null, null);
                                }
                            }
                            else
                            {
                                if (col.DataType == typeof(decimal) && (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?)))
                                {
                                    prop.SetValue(instance, Convert.ToDouble(row[col.ColumnName]), null);
                                }
                                else if (prop.PropertyType == typeof(bool))
                                {
                                    prop.SetValue(instance, Convert.ToBoolean(row[col.ColumnName]), null);
                                }
                                else
                                {
                                    prop.SetValue(instance, row[col.ColumnName], null);
                                }

                            }
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            return instance;
        }

        public static List<T> PopulateObject<T>(ResultTable table)
        {
            List<T> list = new List<T>();
            foreach (RowData row in table)
            {
                list.Add(PopulateObject<T>(row));
            }
            return list;
        }

        private static object PopulateObject(Type t, RowData row)
        {
            object instance = Activator.CreateInstance(t);
            foreach (string prop in row.GetDynamicMemberNames())
            {
                PropertyInfo p = instance.GetType().GetProperty(prop);
                if (p != null && p.CanWrite)
                {
                    object o = row.Column(prop);
                    if (o is ResultTable && p.PropertyType.GetInterface("IList",true) != null)
                    {
                        Type listType = typeof(List<>);
                        Type[] args = p.PropertyType.GetGenericArguments();
                        Type genericList = listType.MakeGenericType(args);
                        object listInstance = Activator.CreateInstance(genericList);
                        foreach (RowData r in (o as ResultTable))
                        {
                            ((IList)listInstance).Add(PopulateObject(args[0], r));
                        }
                        p.SetValue(instance, listInstance);
                    }
                    else
                    {
                        p.SetValue(instance, o);
                    }
                }
            }
            return instance;
        }

        public static T PopulateObject<T>(RowData row)
        {
            return (T)PopulateObject(typeof(T), row);
        }


        public static T PopulateObject<T>(DataTable dt, DataRow row, bool AllowPrivateProperties, bool EnforceTypesafety, bool UseDefaultConstructor = true)
        {
            T instance = default(T);
            if (UseDefaultConstructor)
            {
                instance = Activator.CreateInstance<T>();
                return PopulateObject<T>(instance, dt, row, AllowPrivateProperties, EnforceTypesafety);
            }
            else
            {
                object o = Activator.CreateInstance(typeof(T), new object[] { row });
                if (o == null)
                {
                    o = Activator.CreateInstance(typeof(T), new object[] { dt, row });
                }
                if (o == null)
                {
                    o = Activator.CreateInstance(typeof(T), new object[] { row, dt });
                }
                if (o == null)
                {
                    o = Activator.CreateInstance(typeof(T), new object[] { dt });
                }
                if (o != null)
                {
                    return (T)o;
                }
                else
                {
                    throw new InvalidOperationException(string.Format("The type {0} does not provide a valid constructor for a DataRow and/or DataTable object", typeof(T).FullName));
                }

            }



        }
    }
}
