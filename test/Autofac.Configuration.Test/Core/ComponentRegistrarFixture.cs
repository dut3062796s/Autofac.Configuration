﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Builder;
using Autofac.Configuration;
using Autofac.Configuration.Core;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Microsoft.Framework.ConfigurationModel;
using Moq;
using Xunit;

namespace Autofac.Configuration.Test.Core
{
    public class ComponentRegistrarFixture
    {
        [Fact]
        public void RegisterConfiguredComponents_AllowsMultipleRegistrationsOfSameType()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithJson("ComponentRegistrar_SameTypeRegisteredMultipleTimes.json");
            var container = builder.Build();
            var collection = container.Resolve<IEnumerable<SimpleComponent>>();
            Assert.Equal(2, collection.Count());

            // Test using Any() because we aren't necessarily guaranteed the order of resolution.
            Assert.True(collection.Any(a => a.Input == 5), "The first registration (5) wasn't found.");
            Assert.True(collection.Any(a => a.Input == 10), "The second registration (10) wasn't found.");
        }

        [Fact]
        public void RegisterConfiguredComponents_AutoActivationEnabledOnComponent()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithJson("ComponentRegistrar_EnableAutoActivation.json");
            var container = builder.Build();
            IComponentRegistration registration;
            Assert.True(container.ComponentRegistry.TryGetRegistration(new KeyedService("a", typeof(object)), out registration), "The expected component was not registered.");
            Assert.True(registration.Services.Any(a => a.GetType().Name == "AutoActivateService"), "Auto activate service was not registered on the component");
        }

        [Fact]
        public void RegisterConfiguredComponents_AutoActivationNotEnabledOnComponent()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithJson("ComponentRegistrar_EnableAutoActivation.json");
            var container = builder.Build();
            IComponentRegistration registration;
            Assert.True(container.ComponentRegistry.TryGetRegistration(new KeyedService("b", typeof(object)), out registration), "The expected component was not registered.");
            Assert.False(registration.Services.Any(a => a.GetType().Name == "AutoActivateService"), "Auto activate service was registered on the component when it shouldn't be.");
        }

        [Fact]
        public void RegisterConfiguredComponents_ConstructorInjection()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithXml("ComponentRegistrar_SingletonWithTwoServices.xml");
            var container = builder.Build();
            var cpt = (SimpleComponent)container.Resolve<ITestComponent>();
            Assert.Equal(1, cpt.Input);
        }

        [Fact]
        public void RegisterConfiguredComponents_ExternalOwnership()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithJson("ComponentRegistrar_ExternalOwnership.json");
            var container = builder.Build();
            IComponentRegistration registration;
            Assert.True(container.ComponentRegistry.TryGetRegistration(new TypedService(typeof(SimpleComponent)), out registration), "The expected component was not registered.");
            Assert.Equal(InstanceOwnership.ExternallyOwned, registration.Ownership);
        }

        [Fact]
        public void RegisterConfiguredComponents_LifetimeScope_InstancePerDependency()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithJson("ComponentRegistrar_InstancePerDependency.json");
            var container = builder.Build();
            Assert.NotSame(container.Resolve<SimpleComponent>(), container.Resolve<SimpleComponent>());
        }

        [Fact]
        public void RegisterConfiguredComponents_LifetimeScope_Singleton()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithXml("ComponentRegistrar_SingletonWithTwoServices.xml");
            var container = builder.Build();
            Assert.Same(container.Resolve<ITestComponent>(), container.Resolve<ITestComponent>());
        }

        [Fact]
        public void RegisterConfiguredComponents_MemberOf()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithJson("ComponentRegistrar_MemberOf.json");
            builder.RegisterCollection<ITestComponent>("named-collection").As<IList<ITestComponent>>();
            var container = builder.Build();
            var collection = container.Resolve<IList<ITestComponent>>();
            var first = collection[0];
            Assert.IsType<SimpleComponent>(first);
        }

        [Fact]
        public void RegisterConfiguredComponents_PropertyInjectionEnabledOnComponent()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithJson("ComponentRegistrar_EnablePropertyInjection.json");
            builder.RegisterType<SimpleComponent>().As<ITestComponent>();
            var container = builder.Build();
            var e = container.Resolve<ComponentConsumer>();
            Assert.NotNull(e.Component);
        }

        [Fact]
        public void RegisterConfiguredComponents_PropertyInjectionWithProvidedValues()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithXml("ComponentRegistrar_SingletonWithTwoServices.xml");
            var container = builder.Build();
            var cpt = (SimpleComponent)container.Resolve<ITestComponent>();
            Assert.Equal("hello", cpt.Message);
            Assert.True(cpt.ABool, "The Boolean property value was not properly parsed/converted.");
        }

        [Fact]
        public void RegisterConfiguredComponents_RegistersMetadata()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithJson("ComponentRegistrar_ComponentWithMetadata.json");
            var container = builder.Build();
            IComponentRegistration registration;
            Assert.True(container.ComponentRegistry.TryGetRegistration(new KeyedService("a", typeof(object)), out registration), "The expected service wasn't registered.");
            Assert.Equal(42, (int)registration.Metadata["answer"]);
        }

        [Fact]
        public void RegisterConfiguredComponents_SingleComponentWithTwoServices()
        {
            var builder = EmbeddedConfiguration.ConfigureContainerWithXml("ComponentRegistrar_SingletonWithTwoServices.xml");
            var container = builder.Build();
            container.AssertRegistered<ITestComponent>("The ITestComponent wasn't registered.");
            container.AssertRegistered<object>("The object wasn't registered.");
            container.AssertNotRegistered<SimpleComponent>("The base SimpleComponent type was incorrectly registered.");
            Assert.Same(container.Resolve<ITestComponent>(), container.Resolve<object>());
        }

        private class ComponentConsumer
        {
            public ITestComponent Component { get; set; }
        }

        private interface ITestComponent
        {
        }

        private class SimpleComponent : ITestComponent
        {
            public SimpleComponent()
            {
            }

            public SimpleComponent(int input)
            {
                Input = input;
            }

            public bool ABool { get; set; }

            public int Input { get; set; }

            public string Message { get; set; }
        }
    }
}