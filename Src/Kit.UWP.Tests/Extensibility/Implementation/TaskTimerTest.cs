﻿namespace Microsoft.HockeyApp.Extensibility.Implementation
{
    using System;
#if CORE_PCL || NET45 || NET46 || WINRT || WINDOWS_UWP
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Extensibility.Implementation.Tracing;
    using TestFramework;
#if NET35 || NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if WINDOWS_PHONE || WINDOWS_STORE || WINDOWS_UWP
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
#if WINRT || WINDOWS_UWP
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class TaskTimerTest
    {
        [TestClass]
        public class Delay
        {
            [TestMethod]
            public void DefaultValueIsOneMinuteBecauseItHasToBeSomethingValid()
            {
                var timer = new TaskTimer();
                Assert.Equal(TimeSpan.FromMinutes(1), timer.Delay);
            }

            [TestMethod]
            public void CanBeChangedByConfigurableChannelComponents()
            {
                var timer = new TaskTimer();
                timer.Delay = TimeSpan.FromSeconds(42);
                Assert.Equal(42, timer.Delay.TotalSeconds);
            }

            [TestMethod]
            public void CanBeSetToInfiniteToPreventTimerFromFiring()
            {
                var timer = new TaskTimer();
                timer.Delay = new TimeSpan(0, 0, 0, 0, -1);
                Assert.Equal(new TimeSpan(0, 0, 0, 0, -1), timer.Delay);
            }

            [TestMethod]
            public void ThrowsArgumentOutOfRangeExceptionWhenNewValueIsZeroOrLess()
            {
                var timer = new TaskTimer();
                Assert.Throws<ArgumentOutOfRangeException>(() => timer.Delay = TimeSpan.Zero);
            }

            [TestMethod]
            public void ThrowsArgumentOutOfRangeExceptionWhenNewValueIsMoreThanMaxIntMilliseconds()
            {
                var timer = new TaskTimer();
                Assert.Throws<ArgumentOutOfRangeException>(() => timer.Delay = TimeSpan.FromMilliseconds((double)int.MaxValue + 1));
            }
        }

        [TestClass]
        public class IsStarted
        {
            [TestMethod]
            public void ReturnsFalseIfTimerWasNeverStarted()
            {
                var timer = new TaskTimer();
                Assert.False(timer.IsStarted);
            }

            [TestMethod]
            public void ReturnsTrueWhileUntilActionIsInvoked()
            {
                var timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };

                var actionStarted = new ManualResetEventSlim();
                var actionCanFinish = new ManualResetEventSlim();
                timer.Start(
                    () => Task.Factory.StartNew(
                        () =>
                            {
                                actionStarted.Set();
                                actionCanFinish.Wait();
                            }));

                Assert.True(timer.IsStarted);

                actionStarted.Wait(50);

                Assert.False(timer.IsStarted);

                actionCanFinish.Set();
            }
        }

        [TestClass]
        public class Start
        {
            [TestMethod]
            public void InvokesActionAfterDelay()
            {
                var timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };

                var actionInvoked = new ManualResetEventSlim();
                timer.Start(() => Task.Factory.StartNew(actionInvoked.Set));

                Assert.False(actionInvoked.IsSet);
                Assert.True(actionInvoked.Wait(50));
            }
        }

        [TestClass]
        public class Cancel
        {
            [TestMethod]
            public void AbortsPreviousAction()
            {
                AsyncTest.Run(async () =>
                {
                    var timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };
        
                    bool actionInvoked = false;
                    timer.Start(() => Task.Factory.StartNew(() => actionInvoked = true));
                    timer.Cancel();
        
                    await TaskEx.Delay(20);
        
                    Assert.False(actionInvoked);
                });
            }
        }
    }
}
