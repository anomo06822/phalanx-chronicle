using System;
using System.Reflection;
using PhalanxChronicle.Battle;
using PhalanxChronicle.Core;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class FirstBattleOnboardingControllerTests
    {
        [Fact]
        public void ActiveController_CanHaveNoVisiblePromptBetweenSteps()
        {
            object controller = CreateController("Push toward the gate.");

            Invoke(controller, "MarkSelectedUnit");
            Invoke(controller, "MarkMovedUnit");
            Invoke(controller, "MarkPlayerActionResolved", false);

            Assert.True((bool)GetProperty(controller, "IsActive"));

            object model = Invoke(
                controller,
                "BuildModel",
                true,
                true,
                false,
                false,
                true);

            Assert.Null(model);
        }

        private static object CreateController(string objectiveText)
        {
            Type controllerType = typeof(BattleSimulation).Assembly.GetType(
                "PhalanxChronicle.Battle.FirstBattleOnboardingController",
                throwOnError: true);

            return Activator.CreateInstance(controllerType, objectiveText);
        }

        private static object Invoke(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.NotNull(method);
            return method.Invoke(instance, arguments);
        }

        private static object GetProperty(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.NotNull(property);
            return property.GetValue(instance);
        }
    }
}
