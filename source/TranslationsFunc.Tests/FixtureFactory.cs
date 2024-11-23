using AutoFixture.AutoMoq;
using AutoFixture;

namespace TranslationFunc.Tests
{
    public static class FixtureFactory
    {
        public static Fixture CreateFixture()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            return fixture;
        }
    }
}
