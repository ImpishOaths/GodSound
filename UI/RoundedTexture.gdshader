shader_type canvas_item;

uniform bool left;

void fragment() {
	vec2 pixelPos = UV-vec2(0.5);
	bool capSide = (pixelPos.x > 0.0) != left;
	if(length(pixelPos) > 0.5 && capSide)
	{
		COLOR = vec4(0);
	}
}
