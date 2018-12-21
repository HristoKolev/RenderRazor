#!/usr/bin/env bash

package_dir="./packages";

if [ -d "$package_dir" ]; then
rm $package_dir -rf
fi

dotnet pack -c release -o "../../$package_dir" --include-symbols --include-source
 
nuget push -ApiKey $nuget_key -source https://api.nuget.org/v3/index.json "$package_dir/"
