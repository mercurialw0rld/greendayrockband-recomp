cmake_minimum_required(VERSION 3.25)
project(greendayrockband LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
include(generated/rexglue.cmake)

add_executable(greendayrockband WIN32
    src/main.cpp
    src/startup_compat.cpp
    src/guitar_input.cpp
    src/input_lock.cpp
    src/physical_input.cpp
    src/network_compat.cpp
    src/random_compat.cpp
    src/precise_sleep.cpp
    src/math_compat.cpp
    src/windows_audio.cpp
    src/audio_pacing.cpp
    src/mogg_header_compat.cpp
    src/gpu_wait.cpp
    src/file_trace.cpp
)
rexglue_setup_target(greendayrockband GPU_PLUGINS xenos)

add_library(beatles-input SHARED src/controller_bridge.cpp)
target_link_libraries(beatles-input PRIVATE SDL3::SDL3-static)
target_compile_definitions(beatles-input PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)
target_link_libraries(greendayrockband PRIVATE beatles-input bcrypt synchronization xaudio2 ole32)

add_executable(greenday-content src/content_import.cpp)
target_link_libraries(greenday-content PRIVATE rex::runtime)
target_compile_definitions(greenday-content PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)

target_link_options(greendayrockband PRIVATE /DEBUG /OPT:REF /OPT:ICF)


