sampler2D uImage0 : register(s0);
sampler2D walltarget : register(s1);

float2 targetsize;
float2 screenposition;
float2 screenres;
float2 scenewallpos;
float2 zoom;

float4 MainPS(float4 drawColor : COLOR0, float2 uv : TEXCOORD0, float4 pos : SV_POSITION) : COLOR0{
    float4 tree = tex2D(uImage0, uv) * drawColor;
    
    float2 screenCenter = screenres * 0.5;
    float2 worldPos = (pos.xy - screenCenter) / zoom + screenposition + screenCenter;

    float2 wallCoord = floor(worldPos - scenewallpos);
    float2 wallUV = (wallCoord + 0.5) / targetsize;

    float4 wall = tex2D(walltarget, wallUV);

    tree.rgba *= (1.0 - wall.a);

    return tree;
}

technique Technique1 {
    pass Mask { 
        PixelShader = compile ps_3_0 MainPS(); 
    }
}