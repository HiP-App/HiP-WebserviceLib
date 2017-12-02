Switch ("$env:Build_SourceBranchName")
{
	"master" { dotnet pack "HiP-WebserviceLib.csproj" -o . }
	"develop" { dotnet pack "HiP-WebserviceLib.csproj" -o . --version-suffix "develop" }
	default { exit }
}

$nupkg = (ls *.nupkg).FullName
dotnet nuget push "$nupkg" -k "$env:MyGetKey" -s "$env:NuGetFeed"