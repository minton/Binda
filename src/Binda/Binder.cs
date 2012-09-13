﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Binda.Utilities;

namespace Binda
{
    public class Binder
    {
        readonly Dictionary<Type, BindaStrategy> _strategies; 

        public Binder()
        {
            _strategies = new Dictionary<Type, BindaStrategy>
                              {
                                  {typeof (TextBox), new DefaultBindaStrategy("Text")},
                                  {typeof (CheckBox), new DefaultBindaStrategy("Checked")},
                                  {typeof (RadioButton), new DefaultBindaStrategy("Checked")},
                                  {typeof (DateTimePicker), new DefaultBindaStrategy("Value")},
                                  {typeof (NumericUpDown), new DefaultBindaStrategy("Value")},
                                  {typeof (ComboBox), new ListControlBindaStrategy()},
                              };
        }
        /// <summary>
        /// Add a new BindaRegistration to the Binder.
        /// </summary>
        /// <param name="control">The type of the custom control.</param>
        /// <param name="property">The property used to get/set the value on the control.</param>
        /// <param name="type">The data type of the property.</param>
        [Obsolete("Use AdRegistration(Type, string)")]
        public void AddRegistration(Type control, string property, Type type)
        {
            AddRegistration(control, property);
        }

        /// <summary>
        /// Add a new Default Binda Strategy for a control type
        /// </summary>
        /// <param name="controlType"></param>
        /// <param name="propertyName"></param>
        public void AddRegistration(Type controlType, string propertyName)
        {
            AddRegistration(controlType, new DefaultBindaStrategy(propertyName));
        }

        /// <summary>
        /// Add a Custom Binda Strategy for a control type
        /// </summary>
        /// <param name="controlType"></param>
        /// <param name="strategy"></param>
        public void AddRegistration(Type controlType, BindaStrategy strategy)
        {
            _strategies[controlType] = strategy;
        }

        /// <summary>
        /// Binds an object to a Form via property names.
        /// </summary>
        /// <param name="source">Any POCO.</param>
        /// <param name="destination">A Windows Form</param>
        public void Bind(object source, Form destination)
        {
            Bind(source, destination, Enumerable.Empty<BindaAlias>().ToList());
        }
        /// <summary>
        /// Binds an object to a Form via property names including aliases.
        /// </summary>S
        /// <param name="source">Any POCO.</param>
        /// <param name="destination">A Windows Form.</param>
        /// <param name="aliases">A list of BindaAlias.</param>
        public void Bind(object source, Form destination, IList<BindaAlias> aliases)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            var sourceProperties = source.GetType().GetProperties();
            var controls = destination.GetAllControlsRecursive<Control>().Where(c => _strategies.ContainsKey(c.GetType())).ToList();
            foreach (var control in controls)
            {
                var alias = aliases.FirstOrDefault(x => x.DestinationAlias.ToUpper() == control.Name.ToUpper());
                var controlName = alias == null ? control.Name : alias.Property;

                var sourceProperty = sourceProperties.FirstOrDefault(x => x.Name.ToUpper() == controlName.ToUpper());
                if (sourceProperty == null) continue;

                var strategy = _strategies[control.GetType()];
                if (source.GetType().GetInterfaces().Any(x => x == typeof (INotifyPropertyChanged)))
                    strategy.BindControl(control, source, sourceProperty.Name);
                else
                    strategy.SetControlValue(control, source, sourceProperty.Name);
            }
        }
        /// <summary>
        /// Binds a Form to an object via property names.
        /// </summary>
        /// <param name="source">A Windows Form.</param>
        /// <param name="destination">Any POCO.</param>
        public void Bind(Form source, object destination)
        {
            Bind(source, destination, Enumerable.Empty<BindaAlias>().ToList());
        }
        /// <summary>
        /// Binds a Form to an object via property names including aliases.
        /// </summary>
        /// <param name="source">A Windows Form.</param>
        /// <param name="destination">Any POCO.</param>
        /// <param name="aliases">A list of BindaAlias.</param>
        public void Bind(Form source, object destination, IList<BindaAlias> aliases)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            var properties = destination.GetType().GetProperties().Where(property => property.CanWrite);
            var controls = source.GetAllControlsRecursive<Control>().Where(c => _strategies.ContainsKey(c.GetType())).ToList();
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                var alias = aliases.FirstOrDefault(x => x.Property.ToUpper() == property.Name.ToUpper());
                if (alias != null)
                    propertyName = alias.DestinationAlias;

                var control = controls.FirstOrDefault(x => x.Name.ToUpper() == propertyName.ToUpper());
                if (control == null) continue;

                var strategy = _strategies[control.GetType()];
                var value = strategy.GetControlValue(control);
                property.SetValue(destination, value, null);
            }
        }
    }
}