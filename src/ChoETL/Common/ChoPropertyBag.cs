using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoPropertyBag : DynamicObject //, INotifyPropertyChanged, IEnumerable
    {
        public static readonly dynamic Global = new ChoPropertyBag();

        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();
        private readonly List<object> _array = new List<object>();
        public string Name { get; set; }

        public ChoPropertyBag(string name = null)
        {
            Name = name;
        }

        public void Reset()
        {
            _properties.Clear();
            _array.Clear();
        }

        public IEnumerable<KeyValuePair<string, object>> AsProperties()
        {
            return _properties.AsEnumerable();
        }

        public object[] ToArray()
        {
            return _array.ToArray();
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _properties.Keys;
        }

        public object this[string key]
        {
            get
            {
                ChoGuard.ArgumentNotNullOrEmpty(key, "Key");
                return _properties.ContainsKey(key) ? _properties[key] : null;
            }

            set
            {
                ChoGuard.ArgumentNotNullOrEmpty(key, "Key");
                if (_properties.ContainsKey(key))
                    _properties[key] = value;
                else
                    _properties.Add(key, value);
            }
        }

        private object GetPropertyValue(string name)
        {
            if (!_properties.ContainsKey(name))
                _properties.Add(name, null); //value == null ? new ChoPropertyBag(name) : value);

            return _properties[name];
        }

        private void SetPropertyValue(string name, object value)
        {
            if (value is Delegate)
                return;
            else
            {
                if (!_properties.ContainsKey(name))
                    _properties.Add(name, value); //value == null ? new ChoPropertyBag(name) : value);
                else
                    _properties[name] = value;
            }
        }

        private object GetOrSetArrayValue(int index, object value)
        {
            if (index < _array.Count)
            {
                if (value == null)
                {
                }
                else
                {
                    dynamic x = _array[index];
                    x.Value = value;
                }
                return _array[index];

            }
            else
            {
                while (index >= _array.Count)
                {
                    _array.Add(value == null ? new ChoPropertyBag() : MarshalArrayValue(value));
                }
            }

            return _array[index];
        }

        private void IsValid(object value)
        {
            if (value != null /*&& !(value is Delegate)*/ && !value.GetType().IsSimple())
                throw new ApplicationException("Invalid object passed.");
        }

        private dynamic MarshalArrayValue(object value)
        {
            if (value != null && !(value is ChoPropertyBag))
            {
                //if (value.GetType() != typeof(List<dynamic>))
                //{
                dynamic x = new ChoPropertyBag();
                x.Value = value;

                value = x;
                //}
            }

            return value == null ? new ChoPropertyBag() : value;
        }

        private dynamic Marshal(object value)
        {
            //if (value != null && !(value is ChoPropertyBag))
            //{
            //    //if (value.GetType() != typeof(List<dynamic>))
            //    //{
            //        dynamic x = new ChoPropertyBag();
            //        x.Value = value;

            //        value = x;
            //    //}
            //}

            return value == null ? new ChoPropertyBag() : value;
        }

        /// <summary>
        /// Catch a get member invocation
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = GetMember(binder.Name);
            return true;
        }

        private object GetMember(string name)
        {
            object result;
            if (PreGetMember(name, out result))
                return result;

            return GetPropertyValue(name);
        }

        private bool PreGetMember(string name, out object value)
        {
            value = null;
            if (name == "Name")
            {
                value = Name;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Catch a set member invocation
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            SetMember(binder.Name, value);

            return true;
        }

        private object SetMember(string name, object value)
        {
            //IsValid(value);
            string memberName = name;

            if (PreSetMember(name, value))
                return GetMember(name);

            if (value is ChoPropertyBag)
                SetPropertyValue(memberName, value as ChoPropertyBag);
            else
                SetPropertyValue(memberName, value);

            OnPropertyChanged(memberName);
            return GetMember(name);
        }

        private bool PreSetMember(string name, object value)
        {
            if (name == "Name" && value is string)
            {
                Name = value as string;
                return true;
            }

            return false;
        }

        #region Commented

        /// <summary>
        /// Handle the indexer operations
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="indexes"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            if ((indexes.Length == 1) && indexes[0] != null)
            {
                if (indexes[0] is string)
                {
                    result = GetPropertyValue((string)indexes[0]);
                    return true;
                }
                else if (indexes[0] is int)
                {
                    result = GetOrSetArrayValue((int)indexes[0], null);
                    return true;
                }
            }

            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            IsValid(value);
            if ((indexes.Length == 1) && indexes[0] != null)
            {
                if (indexes[0] is string)
                {
                    SetPropertyValue((string)indexes[0], value);
                    return true;
                }
                else if (indexes[0] is int)
                {
                    GetOrSetArrayValue((int)indexes[0], value);
                    return true;
                }
            }

            return false;
        }

        #endregion Commented

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if (propertyChanged != null) propertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public IEnumerator GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder msg = new StringBuilder(Name.IsNullOrWhiteSpace() ? "ExpandoObject Properties" : "{0} ExpandoObject Properties".FormatString(Name));
            if (_properties != null)
            {
                foreach (string key in _properties.Keys)
                {
                    if (!(_properties[key] is ChoPropertyBag))
                        msg.AppendFormat("{0}: {1}{2}".FormatString(key, ChoUtility.ToStringEx(_properties[key])));
                }
                foreach (string key in _properties.Keys)
                {
                    if (_properties[key] is ChoPropertyBag)
                        msg.AppendFormat("{0}".FormatString(ChoUtility.ToStringEx(_properties[key])));
                }
            }
            return msg.ToString();
        }

        public override int GetHashCode()
        {
            dynamic x = this;
            return x.Value == null ? base.GetHashCode() : x.Value.GetHashCode;
        }

        public override bool Equals(object obj)
        {
            dynamic item = obj as ChoPropertyBag;

            if (item == null)
            {
                return false;
            }

            dynamic x = this;
            return x.Value.Equals(item.Value);
        }

        public override DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MyMetaObject(parameter, this);
        }

    }

    public class MyMetaObject : DynamicMetaObject
    {
        public MyMetaObject(Expression parameter, object value)
            : base(parameter, BindingRestrictions.Empty, value)
        {
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            return this.PrintAndReturnIdentity("InvokeMember of method {0}", binder.Name);
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            // Expression representing the CGValue instance whose member is being set
            Expression instanceExprExpr = Expression.Convert(Expression, LimitType);

            // Expression representing a call to CGValue.Set with the member name
            // and a CGValue wrapping an Expression representing the value to assign
            // to the member.  CGValue.Set will return an expression representing
            // the assignment.
            Expression assignExprExpr = Expression.Call(instanceExprExpr, "SetMember", null,
                Expression.Constant(binder.Name), Expression.Convert(value.Expression, typeof(object)));

            // Package up the result
            return new DynamicMetaObject(assignExprExpr, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            // DynamicMetaObject.Expression is a LINQ expression representing the
            // dynamic object instance (the CGValue instance).  But its declared type
            // is Object; we need to cast it to CGValue before we can call anything
            // on it. 
            Expression instanceExprExpr = Expression.Convert(Expression, LimitType);

            // Build an expression representing a call to CGValue.Get 
            Expression memberExprExpr = Expression.Call(instanceExprExpr, "GetMember", null,
                Expression.Constant(binder.Name));

            // Package it up in a new DynamicMetaObject
            // NOTE: The binding restriction parameter pertains to the dynamic object whose
            // member is being accessed, not the dynamic object resulting from the member
            // access.  So as a general rule, just pass through the binding restrictions
            // for this DynamicMetaObject instance, which matches the dynamic object whose
            // member is being accessed.
            return new DynamicMetaObject(memberExprExpr, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        private DynamicMetaObject PrintAndReturnIdentity(string message, string name)
        {
            Console.WriteLine(String.Format(message, name));
            return new DynamicMetaObject(
                Expression,
                BindingRestrictions.GetTypeRestriction(
                    Expression,
                    typeof(ChoPropertyBag)));
        }
    }
}
