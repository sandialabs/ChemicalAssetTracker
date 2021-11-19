using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;

namespace CMS.Controllers
{
    public class BuildInformation
    {
        public String AssemblyName { get; private set; }
        public String ProductName { get; private set; }
        public String CompanyName { get; private set; }
        public System.Version Version { get; private set; }
        public String Description { get; private set; }
        public String Copyright { get; private set; }
        public DateTime BuildDate { get; private set; }
        public String VersionString { get { return String.Format("{0}.{1}", Version.Major, Version.Minor); } }

        public BuildInformation()
        {
            InitializeFromAssembly(Assembly.GetEntryAssembly());
        }

        public BuildInformation(Assembly assembly)
        {
            InitializeFromAssembly(assembly);
        }

        private void InitializeFromAssembly(Assembly assembly)
        {
            AssemblyName = assembly.GetName().Name;

            AssemblyProductAttribute product = (AssemblyProductAttribute)AssemblyProductAttribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute));
            ProductName = (product == null ? "n/a" : product.Product);

            AssemblyDescriptionAttribute description = (AssemblyDescriptionAttribute)AssemblyDescriptionAttribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute));
            Description = (description == null ? "n/a" : description.Description);

            AssemblyCopyrightAttribute copyright = (AssemblyCopyrightAttribute)AssemblyCopyrightAttribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute));
            Copyright = (copyright == null ? "n/a" : copyright.Copyright);

            AssemblyCompanyAttribute company = (AssemblyCompanyAttribute)AssemblyCompanyAttribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute));
            CompanyName = (company == null ? "n/a" : company.Company);

            Version = assembly.GetName().Version;
            //BuildDate = GetBuildDate(Version);
            BuildDate = GetBuildDate(assembly);

        }


        ///----------------------------------------------------------------
        ///
        /// Function:       GetBuildDate
        /// Author:         Pete Humphrey
        ///
        /// <summary>
        /// Get the build date of an assembly
        /// </summary>
        ///
        /// <param name="assembly">the executing assembly</param>
        /// <returns>a DateTime object</returns>
        /// 
        /// <remarks>
        /// This code depends on a property in the .csproj file (PropertyGroup)
        ///     <SourceRevisionId>build$([System.DateTime]::Now.ToString("yyyyMMddHHmmss"))</SourceRevisionId>
        /// </remarks>
        ///
        ///----------------------------------------------------------------
        private DateTime GetBuildDate(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                    if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    {
                        return result;
                    }
                }
            }

            return new DateTime(2000,1,1);
        }

        private DateTime OldGetBuildDate(System.Version version)
        {
            int build = version.Build;
            int rev = version.Revision;
            TimeSpan span = new TimeSpan(TimeSpan.TicksPerDay * build + TimeSpan.TicksPerSecond * 2 * rev);
            DateTime result = new DateTime(2000, 1, 1).Add(span);
            return result;
        }

        private DateTime OldGetBuildDate(Assembly assembly)
        {
            System.Version version = assembly.GetName().Version;
            return OldGetBuildDate(version);
        }

    }
}
