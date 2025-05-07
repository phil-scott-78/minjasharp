#!/usr/bin/env bash
set -e

# Default parameters
CONFIGURATION="Debug"
SKIP_CPP=false
SKIP_NET=false
SKIP_TESTS=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --configuration|-c)
      CONFIGURATION="$2"
      if [[ "$CONFIGURATION" != "Debug" && "$CONFIGURATION" != "Release" ]]; then
        echo "Error: Configuration must be either 'Debug' or 'Release'"
        exit 1
      fi
      shift 2
      ;;
    --skip-cpp)
      SKIP_CPP=true
      shift
      ;;
    --skip-net)
      SKIP_NET=true
      shift
      ;;
    --skip-tests)
      SKIP_TESTS=true
      shift
      ;;
    *)
      echo "Unknown parameter: $1"
      echo "Usage: ./build.sh [--configuration|-c Debug|Release] [--skip-cpp] [--skip-net] [--skip-tests]"
      exit 1
      ;;
  esac
done

# Set paths
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
CPP_DIR="$SCRIPT_DIR/src/cpp"
CPP_BUILD_DIR="$CPP_DIR/build"
NET_SLN="$SCRIPT_DIR/MinjaSharp.sln"

# Function to check if CMake is installed
check_cmake() {
  if ! command -v cmake &> /dev/null; then
    echo "Error: CMake is required to build the C++ shim but was not found. Please install CMake and add it to your PATH."
    exit 1
  fi
  
  echo "Found CMake: $(cmake --version | head -n 1)"
}

# Function to check if .NET SDK is installed
check_dotnet() {
  if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is required but was not found. Please install .NET SDK and add it to your PATH."
    exit 1
  fi
  
  echo "Found .NET SDK: $(dotnet --version)"
}

# Function to build the C++ shim
build_cpp_shim() {
  echo "Building C++ shim ($CONFIGURATION)..."
  
  # Create build directory if it doesn't exist
  mkdir -p "$CPP_BUILD_DIR"
  
  # Save current directory
  pushd "$CPP_BUILD_DIR" > /dev/null
  
  # Configure CMake
  cmake -DCMAKE_BUILD_TYPE="$CONFIGURATION" ..
  
  # Build with CMake
  cmake --build . --config "$CONFIGURATION"
  
  if [ $? -ne 0 ]; then
    echo "Error: Failed to build C++ shim"
    exit 1
  fi
  
  echo -e "\033[32mC++ shim built successfully\033[0m"
  
  # Restore directory
  popd > /dev/null
}

# Function to build .NET projects
build_dotnet() {
  echo "Building .NET projects ($CONFIGURATION)..."
  
  # Restore packages
  dotnet restore "$NET_SLN"
  
  # Build solution
  dotnet build "$NET_SLN" --configuration "$CONFIGURATION" --no-restore
  
  if [ $? -ne 0 ]; then
    echo "Error: Failed to build .NET projects"
    exit 1
  fi
  
  echo -e "\033[32m.NET projects built successfully\033[0m"
}

# Function to run tests
run_tests() {
  echo "Running tests..."
  
  dotnet test "$NET_SLN" --configuration "$CONFIGURATION" --no-build
  
  if [ $? -ne 0 ]; then
    echo "Error: Tests failed"
    exit 1
  fi
  
  echo -e "\033[32mTests passed successfully\033[0m"
}

# Main build process
echo "Starting MinjaSharp build process..."

# Check prerequisites
if [ "$SKIP_CPP" = false ]; then
  check_cmake
fi

if [ "$SKIP_NET" = false ]; then
  check_dotnet
fi

# Build C++ shim
if [ "$SKIP_CPP" = false ]; then
  build_cpp_shim
fi

# Build .NET projects
if [ "$SKIP_NET" = false ]; then
  build_dotnet
fi

# Run tests
if [ "$SKIP_TESTS" = false ]; then
  run_tests
fi

echo -e "\033[32mMinjaSharp build completed successfully!\033[0m"