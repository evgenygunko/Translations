using AutoFixture.AutoMoq;
using AutoFixture;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TranslatorApp.Tests
{
    public static class FixtureFactory
    {
        public static IFixture Create()
        {
            var fixture = new Fixture();
            fixture.Customize(
                new AutoMoqCustomization
                {
                    ConfigureMembers = true,
                    GenerateDelegates = true
                });
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            return fixture;
        }

        public static IFixture CreateWithControllerCustomizations()
        {
            var fixture = Create();
            fixture.Customize<BindingInfo>(cc => cc.OmitAutoProperties());

            return fixture;
        }
    }
}
