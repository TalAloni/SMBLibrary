using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class ResponseGetDfsReferralTests
    {
        [TestMethod]
        public void ParseResponseGetDfsReferral_HeaderOnly_SetsFields()
        {
            // Arrange
            // PathConsumed = 6, NumberOfReferrals = 0, ReferralHeaderFlags = 0x10
            byte[] buffer = new byte[8];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 0);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0x10);

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual((ushort)6, response.PathConsumed);
            Assert.AreEqual((ushort)0, response.NumberOfReferrals);
            Assert.AreEqual((uint)0x10, response.ReferralHeaderFlags);
            Assert.IsNotNull(response.ReferralEntries);
            Assert.AreEqual(0, response.ReferralEntries.Count);
            Assert.IsNotNull(response.StringBuffer);
            Assert.AreEqual(0, response.StringBuffer.Count);
        }

        [TestMethod]
        public void GetBytes_ForHeaderOnlyResponse_ProducesExpectedBuffer()
        {
            // Arrange
            ResponseGetDfsReferral response = new ResponseGetDfsReferral();
            response.PathConsumed = 6;
            response.NumberOfReferrals = 0;
            response.ReferralHeaderFlags = 0x10;

            // Act
            byte[] buffer = response.GetBytes();

            // Assert
            Assert.AreEqual(8, buffer.Length);
            Assert.AreEqual((ushort)6, Utilities.LittleEndianConverter.ToUInt16(buffer, 0));
            Assert.AreEqual((ushort)0, Utilities.LittleEndianConverter.ToUInt16(buffer, 2));
            Assert.AreEqual((uint)0x10, Utilities.LittleEndianConverter.ToUInt32(buffer, 4));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_WhenBufferIsNull_ThrowsArgumentNullException()
        {
            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenBufferTooSmall_ThrowsArgumentException()
        {
            // Arrange
            byte[] buffer = new byte[7];

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }
    }
}
