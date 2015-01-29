﻿using System;
using System.IO;
using System.Net;
using System.Reflection;
using Autofac.Configuration.Core;
using Microsoft.Framework.ConfigurationModel;
using Xunit;
using ConfigModel = Microsoft.Framework.ConfigurationModel.Configuration;

namespace Autofac.Tests.Configuration.Core
{
    public class ConfigurationExtensionsFixture
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void DefaultAssembly_AssemblyNameEmpty(string value)
        {
            var config = SetUpDefaultAssembly(value);
            Assert.Null(config.DefaultAssembly());
        }

        [Fact]
        public void DefaultAssembly_AssemblyNameMissing()
        {
            var config = new ConfigModel();
            Assert.Null(config.DefaultAssembly());
        }

        [Fact]
        public void DefaultAssembly_AssemblyNotFound()
        {
            var config = SetUpDefaultAssembly("NoSuchAssembly");
            Assert.Throws<FileNotFoundException>(() => config.DefaultAssembly());
        }

        [Fact]
        public void DefaultAssembly_FullAssemblyName()
        {
            var expected = typeof(String).GetTypeInfo().Assembly;
            var config = SetUpDefaultAssembly(expected.FullName);
            Assert.Equal(expected, config.DefaultAssembly());
        }

        [Fact]
        public void DefaultAssembly_NullConfiguration()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => config.DefaultAssembly());
        }

        [Fact]
        public void DefaultAssembly_SimpleAssemblyName()
        {
            var expected = typeof(String).GetTypeInfo().Assembly;
            var config = SetUpDefaultAssembly("mscorlib");
            Assert.Equal(expected, config.DefaultAssembly());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetAssembly_EmptyKey(string key)
        {
            var config = new ConfigModel();
            Assert.Throws<ArgumentException>(() => config.GetAssembly(key));
        }

        [Fact]
        public void GetAssembly_NullConfiguration()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => config.GetAssembly("defaultAssembly"));
        }

        [Fact]
        public void GetAssembly_NullKey()
        {
            var config = new ConfigModel();
            Assert.Throws<ArgumentNullException>(() => config.GetAssembly(null));
        }

        [Fact(Skip = "Still working out how config should look.")]
        public void GetParameters_SimpleParameters()
        {
            var config = LoadEmbeddedXml("ConfigurationExtensions_Parameters.config");
            Assert.False(true);
        }

        private static IConfiguration LoadEmbeddedXml(string configFile)
        {
            var config = new ConfigModel();
            var xmlSource = new XmlConfigurationSource("fake unit test path");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Files/" + configFile))
            {
                typeof(XmlConfigurationSource).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Stream) }, null).Invoke(xmlSource, new object[] { stream });
            }
            var memorySource = new MemoryConfigurationSource(xmlSource.Data);
            config.Add(memorySource);
            return config;
        }

        private static IConfiguration SetUpDefaultAssembly(string assemblyName)
        {
            var source = new MemoryConfigurationSource();
            source.Add("defaultAssembly", assemblyName);
            var config = new ConfigModel();
            config.Add(source);
            return config;
        }

        public class HasSimpleParametersAndProperties
        {
            public HasSimpleParametersAndProperties(int number, IPAddress ip)
            {
                this.Number = number;
                this.IP = ip;
            }

            public int Number { get; private set; }

            public IPAddress IP { get; private set; }
        }
    }
}