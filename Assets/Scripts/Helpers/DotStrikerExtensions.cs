using System.Collections;
using System.Collections.Generic;
using DotStriker.NetApiExt.Generated.Model.pallet_template;
using UnityEngine;
using Unity.VisualScripting;
using Substrate.NetApi.Model.Types.Primitive;

public static class DotStrikerExtensions
{
    public static Coord ToCoord(this Vector2 coord)
    {
        return new Coord
        {
            X = new U32((uint)coord.x),
            Y = new U32((uint)coord.y)
        };
    }

    public static Vector3 ToVector2(this Coord coord)
    {
        return new Vector3(coord.X.ConvertTo<float>(), coord.Y.ConvertTo<float>());
    }

    public static Vector3 ToVector3(this Coord coord)
    {
        return new Vector3(coord.X.ConvertTo<float>(), 0, coord.Y.ConvertTo<float>());
    }

    public static Vector3 ToV3(this Vector2 coord)
    {
        return new Vector3(coord.x, 0, coord.y);
    }
}
