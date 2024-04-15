# Serverless NPM Registry

This is a simple serverless NPM registry that uses AWS Lambda and S3 to store and retrieve packages.
Simple as that, no more, no less.

The API part of the project implements the essential endpoints from the NPM registry API that are necessary to
obtain information on packages, as described in the [NPM registry API documentation](https://github.com/npm/registry/blob/master/docs/REGISTRY-API.md).
Storage of packages is done in S3. For simplicity packages are uploaded directly to S3, without API. This means that
a custom publish script is needed to publish packages to the registry.

## Motivation
The motivation behind this project is to provide a simple way to share private packages between Unity projects. Imagine you
want to keep your code project as a separate Unity project and share the results as a package. You can use this project to
create a private registry where you can publish your packages and then reuse them in your Unity projects.

## Usage
### Deploy registry
1. Install dotnet lambda tools: `dotnet tool install -g Amazon.Lambda.Tools`
2. Modify the `serverless.template` file to set the bucket name where packages will be stored.
3. Deploy: `dotnet lambda deploy-serverless`

### Publish package
1. Create a package: `npm pack`
2. Upload package to S3: `aws s3 cp <package>.tgz s3://<bucket>/<package-id>/<package>.tgz`

Or add a script to your `package.json` (see `examples/package.json`):
```json
{
  "scripts": {
    "publish": "npm pack && aws s3 cp . s3://<bucket>/<package-id>/ --recursive --exclude \"*\" --include \"*.tgz\" && del \"./*.tgz\""
  }
}
```
And then run `npm run publish`.

### Install package
For any normal npm project, add registry to API gateway, as you would do for any other registry.

For Unity projects, add the registry to the `scopedRegistries` in the `Packages/manifest.json` file:
```json
{
  "scopedRegistries": [
    {
      "name": "MyRegistry",
      "url": "https://<api-id>.execute-api.<region>.amazonaws.com/Prod/",
      "scopes": [
        "<package-scope>"
      ]
    }
  ]
}
```

Or alternatively configure scoped registry in Unity Project Settings:
1. Navigate to `Edit -> Project Settings -> Package Manager`.
2. Add the registry URL to the `Scoped Registries` list.
3. Specify the package scope and registry name.
4. Click `Save`.

Then add the package to the `dependencies` in the `Packages/manifest.json` file:
```json
{
  "dependencies": {
    "<package-id>": "<version>"
  }
}
```

Or use the Package Manager window:
1. Open the Package Manager window.
2. Click the `Packages: My Registries` dropdown.
3. Find your package in the list and click `Install`.
