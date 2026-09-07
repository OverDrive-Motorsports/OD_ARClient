using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlaceholderPlayModeTests
{
    [UnityTest]
    public IEnumerator PlayModeRunner_Executes()
    {
        yield return null;
        Assert.Pass();
    }
}
