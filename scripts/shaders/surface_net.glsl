#[compute]
#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

// A binding to the buffer we create in our script
layout(set = 0, binding = 0, std430) restrict buffer ValueBuffer {
    float[] data;
} value_buffer;

layout(set=0, binding=3, std430) restrict buffer NormalBuffer {
    vec3[] normals;
} normal_data;

layout(set=0,  binding=1, std430) restrict buffer VertBuffer {
    vec3[] verts;
}  vert_data;

layout(set=0,  binding=2, std430) restrict buffer ParametersBuffer {
    vec3 size;
    vec3 scale;
} p;

vec3[] DIRECTIONS = {
    vec3(1.0, 0.0, 0.0),
    vec3(0.0, 1.0, 0.0),
    vec3(0.0, 0.0, 1.0),
};

int get_index(int x, int y, int z) {
    return (x + 1) + (y + 1) * int(p.size.x) + (z + 1) * int(p.size.x) * int(p.size.y);
}
int get_index(vec3 position) {
    return get_index(int(position.x), int(position.y), int(position.z));
}

float get_value(vec3 p) {
    return value_buffer.data[get_index(p)];
}

float get_root(float a, float b) {
    return -a / (b - a);
}
vec3 get_normal(vec3 position) {
    vec3 normal = vec3(0.0);
    float a = get_value(position);
    for(int x = 0; x < 2; x++)
    {
        for(int y = 0; y < 2; y++)
        {
            for(int z = 0; z < 2; z++)
            {
                vec3 direction = vec3(x, y, z);
                float value = get_value(position + direction);
                normal += direction * (value - a);
            }
        }
    }


    return normal;
}
// The code we want to execute in each invocation
void main() {
    // gl_GlobalInvocationID.x uniquely identifies this invocation across all work groups

    vert_data.verts[int(gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * p.size.x + gl_GlobalInvocationID.z * p.size.x * p.size.y)] = ((gl_GlobalInvocationID + vec3(1.0) - p.size  / 2.0) / p.scale); 
    // Get average_postion
    vec3 _g_p = gl_GlobalInvocationID - vec3(1.0);
    vec3 _p = vec3(0.0);
    int count = 0;

    normal_data.normals[get_index(_g_p)] = get_normal(_g_p);
    
    if (gl_GlobalInvocationID.x == p.size.x  || gl_GlobalInvocationID.y == p.size.y || gl_GlobalInvocationID.z == p.size.z)
        return;
    for(int x = 0; x < 2; x++)
    {
        for(int y = 0; y < 2; y++)
        {
            for(int z = 0; z < 2; z++)
            {
                if (x + y + z == 0)
                    continue;
                vec3 off = vec3(x, y, z);

                float a = get_value(_g_p);
                float b = get_value(_g_p + off);

                float intersection = get_root(a, b);

                if (intersection < 0 || intersection > 1)
                    continue;

                // Zero along this axis
                _p += off * intersection + _g_p;
                count += 1;
            }

        }
    }
    if (count == 0 )
        return;
    vert_data.verts[get_index(gl_GlobalInvocationID - vec3(1.0))] = ((_p / count)- p.size / 2.0) / p.scale;

}

