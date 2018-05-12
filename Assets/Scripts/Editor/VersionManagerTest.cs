using UnityEngine;
using NUnit.Framework;
using Debug = System.Diagnostics.Debug;

public class VersionManagerTest {

	[Test]
	public void ValidateJsonTest_NoVersion() {
		// Arrange
		var noVersionOutdatedSaveFile = Resources.Load("TestAssets/NoVersionOutdatedSaveFile") as TextAsset;
		Debug.Assert(noVersionOutdatedSaveFile != null, "noVersionOutdatedSaveFile != null");
		var json = VersionManager.CheckAndUpgradeVersion(noVersionOutdatedSaveFile.ToString());

		// Act
		var valid = VersionManager.ValidateSaveFile(json);

		// Assert
		Assert.That(VersionManager.isUpdated && valid);
	}

	[Test]
	public void ValidateJsonTest_NoVersionNoPoints()
	{
		// Arrange
		var noVersionNoPointsOutdatedSaveFile = Resources.Load("TestAssets/NoVersionNoPointsOutdatedSaveFile") as TextAsset;
		Debug.Assert(noVersionNoPointsOutdatedSaveFile != null, "noVersionNoPointsOutdatedSaveFile != null");
		var json = VersionManager.CheckAndUpgradeVersion(noVersionNoPointsOutdatedSaveFile.ToString());

		// Act
		var valid = VersionManager.ValidateSaveFile(json);

		// Assert
		Assert.That(VersionManager.isUpdated && valid);
	}
}
