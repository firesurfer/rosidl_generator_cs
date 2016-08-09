# Copyright 2015 Open Source Robotics Foundation, Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

set(rosidl_generate_interfaces_cs_IDL_FILES
  ${rosidl_generate_interfaces_IDL_FILES})
set(_output_path "${CMAKE_CURRENT_BINARY_DIR}/rosidl_generator_cs/${PROJECT_NAME}")

set(_generated_msg_sources "")
set(_generated_srv_sources "")
foreach(_idl_file ${rosidl_generate_interfaces_cs_IDL_FILES})
  get_filename_component(_parent_folder "${_idl_file}" DIRECTORY)
  get_filename_component(_parent_folder "${_parent_folder}" NAME)
  get_filename_component(_msg_name "${_idl_file}" NAME_WE)
  get_filename_component(_extension "${_idl_file}" EXT)
  string_camel_case_to_lower_case_underscore("${_msg_name}" _header_name)
  
  if(_extension STREQUAL ".msg")
      list(APPEND _generated_msg_sources
        "${_output_path}/${_parent_folder}/${_header_name}.cs"
      )
      ${BIN
  elseif(_extension STREQUAL ".srv")
    list(APPEND _generated_srv_sources
        "${_output_path}/${_parent_folder}/${_header_name}.cs"
      )
  else()
    list(REMOVE_ITEM rosidl_generate_interfaces_cs_IDL_FILES ${_idl_file})
  endif()
 
endforeach()
MESSAGE(${_generated_msg_sources})
