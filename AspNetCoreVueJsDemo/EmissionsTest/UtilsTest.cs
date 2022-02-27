using NUnit.Framework;
using System;

namespace Emissions.Test {
    internal class UtilsTest {
        [Test]
        public void DateTimeConversion() {
            var time = new DateTimeOffset(2022, 02, 22, 02, 02, 02, TimeSpan.Zero);
            Assert.AreEqual(time.Ticks, Utils.Dates.ConvertStringToDateTimeUTC("2022-02-22T02:02:02Z").Ticks);
            Assert.AreEqual(time.Ticks, Utils.Dates.ConvertStringToDateTimeUTC("2022-02-22T02:02:02").Ticks);
            Assert.AreEqual(time.Ticks, Utils.Dates.ConvertStringToDateTimeUTC("2022-02-22T03:02:02+01:00").Ticks);
            Assert.AreEqual(time.Ticks, Utils.Dates.ConvertStringToDateTimeUTC("2022-02-22T01:02:02-01:00").Ticks);
            Assert.AreNotEqual(time.Ticks, Utils.Dates.ConvertStringToDateTimeUTC("2022-02-22T01:02:02-02:00").Ticks);
            Assert.AreNotEqual(time.Ticks, Utils.Dates.ConvertStringToDateTimeUTC("2022-02-22T01:02:02+02:00").Ticks);
            Assert.AreNotEqual(time.Ticks, Utils.Dates.ConvertStringToDateTimeUTC("2022-02-22T01:02:02").Ticks);
        }
    }
}
