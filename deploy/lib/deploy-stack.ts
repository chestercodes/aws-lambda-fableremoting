import { Duration, PhysicalName, RemovalPolicy, Stack, StackProps } from 'aws-cdk-lib';
import { AuthorizationType, IResource, LambdaIntegration, MockIntegration, PassthroughBehavior, RestApi } from 'aws-cdk-lib/aws-apigateway';
import { AllowedMethods, CacheHeaderBehavior, CachePolicy, CacheQueryStringBehavior, Distribution } from 'aws-cdk-lib/aws-cloudfront';
import { S3Origin, HttpOrigin } from 'aws-cdk-lib/aws-cloudfront-origins';
import { Code, Function, Runtime } from 'aws-cdk-lib/aws-lambda';
import { Bucket } from 'aws-cdk-lib/aws-s3';
import { BucketDeployment, Source } from 'aws-cdk-lib/aws-s3-deployment';
import { Construct } from 'constructs';

export class DeployStack extends Stack {
  constructor(scope: Construct, id: string, props?: StackProps) {
    super(scope, id, props);

    const siteBucket = new Bucket(this, "SiteBucket", {
      websiteIndexDocument: 'index.html',
      websiteErrorDocument: 'index.html',
      publicReadAccess: true,
      autoDeleteObjects: true,
      removalPolicy: RemovalPolicy.DESTROY,
      bucketName: PhysicalName.GENERATE_IF_NEEDED,
    });

    const api = new RestApi(this, 'RestApi', {
      restApiName: "fableremoting",
      deployOptions: {
        stageName: "api"
      },
    })

    const cachePolicy = new CachePolicy(this, "CacheApiPolicy", {
      defaultTtl: Duration.seconds(0),
      headerBehavior: CacheHeaderBehavior.allowList("Authorization", "Content-Type"),
      queryStringBehavior: CacheQueryStringBehavior.all(),
    })

    const restApiDomain = api.url.replace("https://", "").split('/')[0]

    const distribution = new Distribution(this, "SiteDistribution",
      {
        defaultRootObject: "index.html",
        defaultBehavior: {
          origin: new S3Origin(siteBucket, {}),
        },
        additionalBehaviors: {
          "/api/*": {
            // Don't use RestOrigin as this adds a originPath
            origin: new HttpOrigin(restApiDomain, {}),
            allowedMethods: AllowedMethods.ALLOW_ALL,
            cachePolicy,
          }
        }
      }
    );


    const apiFunction = new Function(this, 'ApiFunction', {
      code: Code.fromAsset("../server/dist"),
      handler: 'Server::Server.LambdaEntryPoint::FunctionHandlerAsync',
      runtime: Runtime.DOTNET_CORE_3_1,
      environment: {
        LOG_REQUEST: "false",
      },
      functionName: "fableremoting-api",
      memorySize: 128,
      // cold starts can be ~4s and the default timeout is 3s
      timeout: Duration.seconds(10),
    });


    // these resources need to match the method names in shared/Api.fs -> IServerApi
    const counterResource = api.root.addResource('Counter');
    addCorsOptions(counterResource)

    counterResource.addMethod('GET', new LambdaIntegration(apiFunction), {
      authorizationType: AuthorizationType.NONE,
    })

    const printResource = api.root.addResource('Print');
    addCorsOptions(printResource)

    printResource.addMethod('POST', new LambdaIntegration(apiFunction), {
      authorizationType: AuthorizationType.NONE,
    })


    new BucketDeployment(this, 'DeployWithColourInvalidation', {
      sources: [Source.asset("../client/dist")],
      destinationBucket: siteBucket,
      distribution,
      distributionPaths: [`/*`],
    })

  }
}


export function addCorsOptions(apiResource: IResource) {
  apiResource.addMethod('OPTIONS', new MockIntegration({
    integrationResponses: [{
      statusCode: '200',
      responseParameters: {
        'method.response.header.Access-Control-Allow-Headers': "'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,X-Amz-User-Agent'",
        'method.response.header.Access-Control-Allow-Origin': "'*'",
        'method.response.header.Access-Control-Allow-Credentials': "'false'",
        'method.response.header.Access-Control-Allow-Methods': "'OPTIONS,GET,PUT,POST,DELETE'",
      },
    }],
    passthroughBehavior: PassthroughBehavior.NEVER,
    requestTemplates: {
      "application/json": "{\"statusCode\": 200}"
    },
  }), {
    methodResponses: [{
      statusCode: '200',
      responseParameters: {
        'method.response.header.Access-Control-Allow-Headers': true,
        'method.response.header.Access-Control-Allow-Methods': true,
        'method.response.header.Access-Control-Allow-Credentials': true,
        'method.response.header.Access-Control-Allow-Origin': true,
      },
    }]
  })
}
