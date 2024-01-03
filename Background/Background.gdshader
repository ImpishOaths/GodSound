shader_type canvas_item;

uniform vec2 resolution;
uniform sampler2D rainbow;
uniform sampler2D noise;

void fragment() {
	vec2 sUV = UV*resolution/max(resolution.x,resolution.y);
	float tVal = fract(TIME/30.);
	float tPos = (sUV.x+sUV.y)/2.;
	float nVal = texture(noise, fract(sUV-tVal*2.)).r*2.-1.;
	vec2 texPos = vec2(fract(tPos+tVal))+vec2(nVal)/10.;
	vec2 fractPos = vec2(fract(texPos.x),fract(texPos.y));
	COLOR = texture(rainbow,fractPos);
}
