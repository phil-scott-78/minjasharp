cmake_minimum_required(VERSION 3.14)
project(minja_shim_ext)

cmake_policy(VERSION ${CMAKE_MINIMUM_REQUIRED_VERSION})

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(FetchContent)

# --- minja Dependency ---
FetchContent_Declare(
  minja
  GIT_REPOSITORY "https://github.com/google/minja"
  GIT_TAG main
  GIT_SHALLOW TRUE
  # Add EXCLUDE_FROM_ALL to prevent building unless explicitly requested
  EXCLUDE_FROM_ALL TRUE
)

# Explicitly control what happens with the minja subproject.
# We'll populate it first, then add its library target manually,
# excluding its tests directory.
FetchContent_GetProperties(minja)
if(NOT minja_POPULATED)
  FetchContent_Populate(minja) # Just downloads and extracts
endif()

# Disable minja tests, examples, and python venv creation (drop Python requirements)
set(MINJA_TEST_ENABLED OFF CACHE BOOL "Disable minja tests and python requirements" FORCE)
set(MINJA_EXAMPLE_ENABLED OFF CACHE BOOL "Disable minja examples" FORCE)
set(MINJA_USE_VENV OFF CACHE BOOL "Disable minja python venv creation" FORCE)

# Add the minja library itself, but *not* its tests subdirectory
# minja_SOURCE_DIR is set by FetchContent_GetProperties/Populate
add_subdirectory(${minja_SOURCE_DIR} ${minja_BINARY_DIR} EXCLUDE_FROM_ALL)
# The line above will process minja's main CMakeLists.txt.
# If minja's CMakeLists.txt uses `add_subdirectory(tests)` and respects an option like MINJA_BUILD_TESTS,
# we still need to set that option.
set(MINJA_BUILD_TESTS OFF CACHE BOOL "Disable minja tests" FORCE)


# --- nlohmann/json Dependency ---
FetchContent_Declare(
  json
  URL https://github.com/nlohmann/json/releases/download/v3.11.2/json.tar.xz
  URL_HASH SHA256=583bcf303a5a4b37937d768f208f596f760dd129fd95c462c628a6885741be5d
  DOWNLOAD_EXTRACT_TIMESTAMP TRUE
)
FetchContent_MakeAvailable(json)


# Add source files for our library
set(SOURCES
    minja_shim_ext.cpp
)

# Create shared library
add_library(minja_shim_ext SHARED ${SOURCES})

# --- Platform and Architecture Specific Configurations ---
if(CMAKE_SYSTEM_NAME STREQUAL "Darwin" AND CMAKE_OSX_ARCHITECTURES MATCHES "arm64")
    set_target_properties(minja_shim_ext PROPERTIES OUTPUT_NAME minja_shim_ext_arm64)
endif()

if(WIN32)
    set_target_properties(minja_shim_ext PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
        ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
    )
else()
    set_target_properties(minja_shim_ext PROPERTIES
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
        ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
    )
endif()
# --- END: Platform and Architecture Specific Configurations ---

# Link with dependencies
# 'minja' is the target name defined in minja's CMakeLists.txt
# 'nlohmann_json::nlohmann_json' is the target from json's CMakeLists.txt
target_link_libraries(minja_shim_ext PRIVATE minja nlohmann_json::nlohmann_json)

# Ensure minja's include directories are available to minja_shim_ext
# This might be needed if add_subdirectory doesn't fully propagate transitive INTERFACE properties
# like FetchContent_MakeAvailable would.
target_include_directories(minja_shim_ext PRIVATE ${minja_SOURCE_DIR}/include)