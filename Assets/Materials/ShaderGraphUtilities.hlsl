#if UNITY_ANY_INSTANCING_ENABLED

StructuredBuffer<float4> _ColorBuffer;
uint _InstanceIDOffset;

void GetInstanceColor_float(out float4 color)
{
    color = _ColorBuffer[unity_InstanceID + _InstanceIDOffset];
}

#else

void GetInstanceColor_float(out float4 color)
{
    color = float4(0, 0, 0, 1);
}

#endif
