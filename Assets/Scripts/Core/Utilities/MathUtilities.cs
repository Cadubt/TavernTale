using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtilities
{
    public static int Clamp(int value, int min, int max)
    {
        return Mathf.Clamp(value, min, max);
    }
}