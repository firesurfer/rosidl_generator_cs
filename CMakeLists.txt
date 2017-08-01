cmake_minimum_required(VERSION 3.5)

project(rosidl_generator_cs C)



find_package(ament_cmake REQUIRED)
find_package(ament_cmake_python REQUIRED)

find_package(dotnet_cmake_module REQUIRED)
find_package(DotNETExtra MODULE)

file(GLOB_RECURSE CS_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/src/*.cs)
    
add_assemblies(rosidl_generator_cs
    ${CS_SOURCES}
    OUTPUT_NAME
    "rosidl_generator_cs"
    OUTPUT_TYPE 
    "Exe"
    COMPILER_ARGS
    "-main:ROS2CSMessageGenerator.MainClass"
    REFERENCE_ASSEMBLIES
    "System,System.Xml,System.Xml.Linq"
)
#add_custom_target(ros2cs_message_generator ALL
#	COMMAND 
#	"xbuild" ROS2CSMessageGenerator.csproj /property:OutputPath=${CMAKE_BINARY_DIR} /property:AssemblyName=rosidl_generator_cs 
#	WORKING_DIRECTORY
#	${CMAKE_SOURCE_DIR}/src
#)

ament_export_dependencies(rosidl_cmake)


if(BUILD_TESTING)
  find_package(ament_lint_auto REQUIRED)
  ament_lint_auto_find_test_dependencies()

  set(message_files
    "msg/Empty.msg"
    "msg/Nested.msg"
    "msg/Primitives.msg"
    "msg/Various.msg"
    "msg/Bool.msg"
    "msg/Byte.msg"
    "msg/Char.msg"
    "msg/Float32.msg"
    "msg/Float64.msg"
    "msg/Int8.msg"
    "msg/Int16.msg"
    "msg/Int32.msg"
    "msg/Int64.msg"
    "msg/Uint8.msg"
    "msg/Uint16.msg"
    "msg/Uint32.msg"
    "msg/Uint64.msg"
    "msg/PrimitiveValues.msg"
    "msg/Strings.msg"
    "msg/PrimitivesUnboundedArrays.msg"
    "msg/PrimitivesBoundedArrays.msg"
    "msg/PrimitivesStaticArrays.msg"
    "msg/Telegram1.msg"
    "msg/Telegram2.msg"
    "msg/Wire.msg"
    "msg/Constants.msg"
  )

  include(cmake/register_cs.cmake)
  set(rosidl_generator_cs_DIR "${CMAKE_CURRENT_SOURCE_DIR}/cmake")
  set(rosidl_generator_cs_LIBRARIES "${PROJECT_NAME}")

  rosidl_generator_cs_extras(
    "${CMAKE_CURRENT_BINARY_DIR}/rosidl_generator_cs.exe"
  )

  rosidl_generate_interfaces(${PROJECT_NAME}_interfaces ${message_files}
    SKIP_INSTALL
  )
endif()

ament_package(
  CONFIG_EXTRAS "rosidl_generator_cs-extras.cmake.in"
)
install(FILES ${CMAKE_BINARY_DIR}/rosidl_generator_cs.exe DESTINATION lib/rosidl_generator_cs)

#install_assemblies(ros2cs_message_generator COPY_DEPENDENCIES DESTINATION lib/rosidl_generator_cs)
install(
  DIRECTORY cmake 
  DESTINATION share/${PROJECT_NAME}
)
