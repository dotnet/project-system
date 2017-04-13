// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.MockObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Reflection;


namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    /// <summary>
    /// An IServiceProvider mock class with default but replacable and expandable
    ///   set of services exposed.
    /// </summary>
    public class ServiceProviderMock : SequenceMock<IServiceProvider>
    {
        // Available services
        Dictionary<Type, object> m_services = new Dictionary<Type, object>();

        /// <summary>
        /// Constructor
        /// </summary>
        public ServiceProviderMock()
        {
            //Implementation of GetService
            Implement("GetService",
                new object[] { MockConstraint.IsAnything<Type>() },
                delegate(object obj, MethodInfo method, object[] arguments)
                {
                    Type serviceType = (Type)arguments[0];
                    if (m_services.ContainsKey(serviceType))
                    {
                        return m_services[serviceType];
                    }
                    else
                    {
                        throw new ArgumentException("serviceType '" + serviceType.FullName + "' not implemented in Mocks.ServiceProviderMock()");
                    }
                }
            );

            // Default services (can be replaced)
            Fake_AddService(typeof(IUIService), new UIServiceMock().Instance); //UNDONE: don't do this by default
        }


        /// <summary>
        /// Add (or replace) a service implementation
        /// </summary>
        /// <param name="service"></param>
        public void Fake_AddService<T>(T service)
        {
            Fake_AddService(typeof(T), service);            
        }

        /// <summary>
        /// Add (or replace) a service implementation
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="service"></param>
        public void Fake_AddService(Type serviceType, object service)
        {
            m_services[serviceType] = service;
        }

        /// <summary>
        /// Add (or replace) a service implementation
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="service"></param>
        public void Fake_RemoveService(Type serviceType)
        {
            m_services[serviceType] = null;
        }

        public void Fake_AddUiServiceFake()
        {
            Fake_AddService(typeof(IUIService), new UIServiceFake());
        }
    }
}
