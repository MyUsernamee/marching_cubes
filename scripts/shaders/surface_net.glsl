#[compute]
#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

// A binding to the buffer we create in our script
layout(set = 0, binding = 0, std430) restrict buffer MyDataBuffer {
    float[] data;
}
my_data_buffer;

layout(set=0,  binding=1, std430) restrict buffer VertBuffer {
    vec3[] verts;
}  vert_data;

layout(set=0,  binding=2, std430) restrict buffer ParametersBuffer {
    vec3 size;
    vec3 scale;
} p;


// The code we want to execute in each invocation
void main() {
    // gl_GlobalInvocationID.x uniquely identifies this invocation across all work groups
    vert_data.verts[int(gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * p.size.x + gl_GlobalInvocationID.z * p.size.x * p.size.y)] = ((gl_GlobalInvocationID + vec3(1.0) - p.size  / 2.0) / p.scale); 
}
