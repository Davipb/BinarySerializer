﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BinarySerialization.Test.Subtype
{
    [TestClass]
    public class SubtypeFactoryTests : TestBase
    {
        [TestMethod]
        public void SubtypeFactoryTest()
        {
            var expected = new SubtypeFactoryClass
            {
                Value = new SubclassB()
            };

            var actual = Roundtrip(expected);
            Assert.AreEqual(2, actual.Key);
            Assert.AreEqual(expected.Value.GetType(), actual.Value.GetType());
        }

        [TestMethod]
        public void SubtypeMixedTest()
        {
            var expected = new SubtypeMixedClass
            {
                Value = new SubclassB()
            };

            var actual = Roundtrip(expected);
            Assert.AreEqual(2, actual.Key);
            Assert.AreEqual(expected.Value.GetType(), actual.Value.GetType());
        }

        [TestMethod]
        public void SubtypeMixedTest2()
        {
            var expected = new SubtypeMixedClass
            {
                Value = new SubSubclassC()
            };

            var actual = Roundtrip(expected);
            Assert.AreEqual(3, actual.Key);
            Assert.AreEqual(expected.Value.GetType(), actual.Value.GetType());
        }
    }
}
