# Content Reactor: Serverless Microservice Sample for Azure

In this sample, we have built four microservices that use an [Event Grid](https://docs.microsoft.com/en-us/azure/event-grid/overview) custom topic for inter-service eventing, and a front-end Angular.js app that uses [SignalR](https://www.asp.net/signalr) to forward Event Grid events to the user interface in real time.

The application itself is a personal knowledge management system, and it allows users to upload text, images, and audio and for these to be placed into categories. Each of these types of data is managed by a dedicated microservice built on Azure serverless technologies including [Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview) and [Cognitive Services](https://docs.microsoft.com/en-us/azure/cognitive-services/welcome). The web front-end communicates with the microservices through a SignalR-to-Event Grid bridge, allowing for real-time reactive UI updates based on the microservice updates. Each microservice is built and deployed independently using VSTS’s build and release management system, and use a variety of Azure-native data storage technologies.

The sample can be built and run by following the instructions in the [`setup.md` file](setup.md).

## Microservices

The back-end components of this sample are written as microservices. Microservice architectures have the benefit that complex systems can be be decomposed into modular, granular, independently written and deployable pieces. Each microservice has a defined domain of responsibility, and can be designed and written using appropriate technologies and architectural patterns that make sense for that particular domain. In a large organisation, different teams may be responsible for different microservices, allowing for each microservice to be built in parallel.

Microservice architectures have a number of characteristics, including:

* **Defined domain:** Each microservice has a defined domain of responsibility (sometimes referred to as a *bounded context*). The microservice manages this domain itself, without concerning itself about other domains.
* **Self-contained:** Each microservice is a self-contained unit. It may contain multiple components that all work together.
* **Independently deployable:** Each microservice can be built and deployed as an independent entity. Deploying one microservice does not affect another microservice.
* **Manages data stores:** The data store or stores used by each microservice should be contained within the microservice boundary, thereby ensuring that there are no hidden dependencies caused by data stores being shared.
* **Loosely coupled:** Microservices should be loosely coupled, and ideally communication will occur asynchronously using event sourcing or queues.
* **Highly automated:** The build, deployment, and ongoing management of microservices should emphasise automation wherever possible.
* **Uses appropriate technologies:** The developers of each microservice can select the appropriate technologies that make sense for its domain. Some microservices may best be built with functional languages while others use imperative or general-purpose languages; some may be built using container technology or [Service Fabric](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-overview-microservices#service-fabric-as-a-microservices-platform) while others make sense to be built using serverless and PaaS technology. This sample demonstrates how a loosely coupled microservice architecture can be built using Azure Functions and other Azure platform-as-a-service (PaaS) and serverless components.

More information on the overall philosophy behind microservices is available from [Sam Newman's _Principles of Microservices_ talk](https://samnewman.io/talks/principles-of-microservices/).

## Communication Between Microservices

A key tenet of microservice architecture is that microservices are **loosely coupled**. While microservices can communicate together, communication should be fairly lightweight and should be conducted in a well-defined manner. If *synchronous (real-time) communication* is necessary then it should take place through well-known, published APIs and should not involve direct remote procedure calls or direct access to data stores or internal components of the microservice. Where possible, asynchronous communication should be used to choreograph multiple microservices.

[Azure provides a number of different asynchronous messaging services](https://azure.microsoft.com/en-us/blog/events-data-points-and-messages-choosing-the-right-azure-messaging-service-for-your-data/), including queues (e.g. [Azure Storage queues](https://docs.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction) and [Service Bus](https://docs.microsoft.com/en-us/azure/service-bus/) queues and topics), streams (e.g. [Event Hubs](https://docs.microsoft.com/en-us/azure/event-hubs/)), and events (e.g. [Event Grid](https://docs.microsoft.com/en-us/azure/event-grid/)). An event-based architecture is a common way to allow microservices to inform other microservices of their activities, and in this sample we use Event Grid.

Event Grid allows for events to be published to a common store (*topic*), and distributed to any interested parties (*subscribers*) who may need to know about the events. Communication - both publishing and subscribing - occurs using HTTP, which means that Event Grid messages can be published and consumed by clients on virtually any platform, including on Azure Functions. Events contain a common set of metadata as well as any custom data relevant to the business domain, and Event Grid provides the ability to apply filters to subscriptions so that only relevant events are forwarded to each microservice. Finally, as a managed service, Event Grid provides a strong SLA and automatically handles data integrity as well as retries failed deliveries.

## Sample Architecture

The sample contains four microservices, each with defined responsibilities: one for managing categories, one for audio notes, one for image notes, and one for text notes. The microservices each contain:

* A front-end API for users to manage their data, built on Azure Functions and using many RESTful design principles;
* A data store, owned and managed by the microservice itself, that is appropriate for the data being stored;
* Back-end APIs as needed, with Event Grid subscription triggers.

Event Grid is used to allow for loosely coupled communication between the microservices. The sample contains an Event Grid custom topic, and each microservice publishes events to the topic as they occur. The back-end APIs subscribe to particular types of events from the topic and process them according to their own business rules.

Additionally, there is a single [Azure Functions Proxies](https://docs.microsoft.com/en-us/azure/azure-functions/functions-proxies) application that exposes the microservices' front-end APIs.

The sample also contains an Angular.js application that is bundled within an ASP.NET WebApp which serves as a middleware to enable bi-directional communication between the front end browser and the Azure functions backend.

![Architecture Diagram](/_docs/architecture.png)

Each microservice and other components within the sample are designed to be built and deployed in a fully automated manner. We used VSTS, although any other build and release management systems could also be used. We used [VSTS's Build YAML](https://docs.microsoft.com/en-us/vsts/build-release/actions/build-yaml?view=vsts) feature to declaratively specify our build process and used [VSTS Release Management](https://docs.microsoft.com/en-us/vsts/build-release/concepts/definitions/release/what-is-release-management?view=vsts) to define our release process and publish the built components to Azure. Please see the `setup.md` file for more information on how to build and release the sample components for yourself.

(TODO Thiago - video could go here?)

## Components

### Event Grid Topic

[Event Grid](https://docs.microsoft.com/en-us/azure/event-grid/) is the messaging technology used by the microservices for orchestration. Each microservice publishes events onto an [Event Grid custom topic](https://docs.microsoft.com/en-us/azure/event-grid/post-to-custom-topic). Microservices, and the front end components, subscribe to this Event Grid custom topic and perform their own business logic based on the events they process.

In this sample, the Event Grid custom topic is in its own resource group, `ContentReactor-Events`.

### Categories Microservice

The categories microservice is responsible for tracking the categories that notes are placed in, as well as providing an index of the notes within each category. The microservice provides an API for creating and managing categories, and the category data is stored within a Cosmos DB collection.

When a new category is created, or the name of a category is updated, the categories API processes the change and publishes an event onto the Event Grid custom topic. Event Grid then forwards these events to the categories microservice's worker API, which has two Event Grid subscribers: the `UpdateCategorySynonyms` function will communicate with [Big Huge Thesaurus's API](https://words.bighugelabs.com/) to find a set of synonyms for the category, and the `AddCategoryImage` function will communicate with the [Bing Image Search API](https://azure.microsoft.com/en-us/services/cognitive-services/bing-image-search-api/) to conduct an image search and randomly choose an image to represent the category.

When notes are created, updated, or deleted in other microservices, the categories worker API has Event Grid subscriber functions to update the item list for that category.

The categories microservice maintains a preview for each note. For image notes, this is a link to the preview image. For text notes, this is the first 100 characters of the note. For audio notes, this is the first 100 characters of the audio transcript.

The categories microservice components are in a resource group named `ContentReactor-Categories`.

### Images Microservice

The images microservice maintains the image notes that a user creates. Images up to 4MB in size can be uploaded to the microservice's API. The images microservice takes responsibility for creating a small preview of the image, and for obtaining a description of the image from Azure Cognitive Services. Images are stored within an Azure Storage blob collection.

When an image is uploaded, the images API stores the image in the `fullimages` blob collection, and also creates a preview image to store in a separate `previewimages` blob collection. An `ImageCreated` event is then published onto the Event Grid custom topic. Event Grid forwards the event to the categories microservice, as well as to the images microservice's worker API, which has a function `UpdateImageCaption` that uses [Azure Cognitive Services' Computer Vision API](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/) to obtain a caption for the image. This is then saved to the blob metadata and is returned in subsequent requests to the microservice's API.

When images are updated (e.g. when the caption is updated) or deleted, the microservice publishes events that are used by the categories microservice as well as any other interested components.

The images microservice components are in a resource group named `ContentReactor-Images`.

### Audio Microservice

The audio microservice maintains the audio notes that a user creates. Short audio files of less than 15 seconds duration can be uploaded to the microservice's API. The audio microservice stores the files within an Azure Storage blob collection and then takes responsibility for transcribing the audio to text.

When an audio file is uploaded, the audio API stores the file in the `audio` blob collection, and publishes an `AudioCreated` event to the Event Grid custom topic. Event Grid then forwards the event to the categories microservice, as well as the audio microservice's worker API, which has a function `UpdateAudioTranscript` that uses [the Bing Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/speech/) to obtain a transcript for the audio. This is then saved to the blob metadata and is returned in subsequent requests to the microservice's API.

When audio files are uploaded (i.e. when the transcript is updated) or deleted, the microservice publishes events that are used by the categories microservice as well as any other interested components.

The audio microservice components are in a resource group named `ContentReactor-Audio`.

### Text Microservice

The text microservice maintains the text notes that a user creates. Text can be uploaded to the microservice's API, and the microservice stores it in a Cosmos DB collection.

When a text note is submitted to the API, the text API stores the text and then publishes a `TextCreated` event to the Event Grid custom topic. Event Grid then forwards the event to the categories microservice.

When text notes are updated or deleted, the microservice publishes events that are used by the text microservice as well as any other interested components.

The audio microservice components are in a resource group named `ContentReactor-Text`.

### Proxies

Each of the microservices presents a public-facing API for working with the resources they manage. In keeping with best practices for exposing multiple microservice-hosted APIs, the sample includes an API proxy that presents the four APIs at a single hostname. This simplifies the client side, ensuring it only has to know about a single base URL. The API proxy is built using [Azure Functions Proxies](https://docs.microsoft.com/en-us/azure/azure-functions/functions-proxies).

### Front-End

The front-end is built as an Angular application that is bundled inside an ASP.NET web app. The ASP.NET web app plays the following roles:

* Uses SignalR to push notifications from Event Grid up to the front-end Angular app served in a browser.
* Provides APIs that in turn call the Microservices APIs.

The front-end uses the [@aspnet/signalr NPM package](https://www.npmjs.com/package/@aspnet/signalr), which encapsulates connections to the SignalR Hub.

In order to remove dependencies between UI component frameworks and Angular, the front end HTML/CSS uses plain and simple Bootstrap and is built with Angular 5 using the Angular CLI. This reduces dependency restrictions on any UI component frameworks.

The front-end Angular application is built as a standalone application using npm commands and then bundled with the ASP.NET web app through MSBuild scripts. This way, the front-end framework can be easily swapped with another framework (e.g. React.js) in future. No coupling exists between the front-end and the ASP.NET Middleware through code dependencies.

The front-end is designed to expect responses as part of CRUD operations and listen to events that are consequences of the operations. Alternately, it can also be used in a 'fire and forget' fashion, where the front-end can fire any CRUD event and then wait for SignalR/Event Grid notifications that it uses to update itself.

The front-end consists of the following scaffolding from Angular CLI and the app folder is further customized:

* **Login Component:** This component is responsible for logging in a user, by setting up connections to the SignalR Hub for the user. It also deals with routing to the next page after a successful login. See the _User Authentication and Authorization_ section below for more information.
* **Category Component:** This deals with category CRUD operations and uses observables to bind the items within a category.
* **Item Component:** This component represents image, text, and audio items where each item is represented by its own model classes.
* **Hub Service:** This service is responsible for creating the connection to the SignalR hub and serves as an injectable service to any component.
* **Data Service:** This service holds data that needs to be shared among components, as observables. For example, once a user is successfully logged in, the `userId` is stored in this service as an observable and can be accessed by other components and services.

The `app.module.ts` file bootstraps the application.

Note: The front end is not optimized for mobile browsers.

## Middleware

The front-end and the Azure Functions/Event Grid backend are connected by ASP.NET middleware that uses the latest [preview version of .NET Core](https://blogs.msdn.microsoft.com/webdev/2018/02/27/asp-net-core-2-1-0-preview1-using-asp-net-core-previews-on-azure-app-service/).

The ASP.NET web app maintains a list of users and a list of connection IDs per user, as a user could be logged in from multiple devices. It registers itself as a webhook-based Event Grid subscription and pushes notifications to the front end through a SignalR Hub called the `EventHub`.

The Hub Context is then used by other applications to send and receive data from the hub.

The connection between the SignalR front-end and backend is refreshed on each disconnection event to enable a fully connected scenario. After reconnection, the connection IDs for a user are updated by the middleware.

Note:

* The latest ASP.NET Core 2.1 preview version bundles the SignalR library, within the [AspNetCore.All NuGet package](https://www.nuget.org/packages/Microsoft.AspNetCore.All).
* The ASP.NET SignalR component is wired for bidirectional communication. The front-end-to-backend call is designed as a typical controller API call, but it is also possible to invoke the backend directly via SignalR.
* The front end/middleware and backend can be substituted with other frameworks. For example, React.js/Socket.io and Node.js can also be a combination respectively.

### User Authentication and Authorization

This sample allows for multiple users to store and retrieve notes. Each microservice is responsible for using the appropriate tools at its disposal for ensuring separation between users; the categories and text microservices use [Cosmos DB's partitioned collections](https://docs.microsoft.com/en-us/azure/cosmos-db/partition-data) feature to store each user's data on a separate partition, while the images and audio microservices use [Azure Storage's](https://docs.microsoft.com/en-us/azure/storage/) blob folders to achieve a similar goal. The front-end APIs on each microservice accept the user's ID in their query strings and perform operations on the data in the context of that user.

We have not implemented a user management system, nor any form of true authentication. In the sample, the front-end UI prompts the user to create an account and assigns a user ID, but there is no validation of users (e.g. using a password or federated identity). The microservice APIs assume that the user's identity has been fully verified by the front-end UI, which is not in keeping with best security practices and would need to be updated before being production ready.

While it's out of the scope of this sample, it would certainly be possible to build authentication and user identity handling into the system. There are a number of approaches that could be used including adding federated identity and using Azure AD B2C.

### Approach 1: Federated Identity

A number of public identity providers, such as Facebook, Twitter, Google, or Microsoft's Live ID, can be used to provide identity services to the Content Reactor system. In general, the following steps could be followed to use this type of federated identity approach:

 1. Set up a client application within the relevant identity provider. For example, [this page contains instructions on creating a Facebook application](https://docs.microsoft.com/en-us/azure/app-service/app-service-mobile-how-to-configure-facebook-authentication#a-nameregister-aregister-your-application-with-facebook).
 2. Update the front-end application to obtain a token from the identity provider, using the relevant OpenID Connect flow, and attach it to all API requests.
 3. Set up each microservice's front-end API Functions app to check for authorization. [This blog post](https://blogs.msdn.microsoft.com/stuartleeks/2018/02/19/azure-functions-and-app-service-authentication/) provides some helpful advice on how to authenticate tokens from within an Azure Functions app.
 4. Update each microservice's front-end API Functions app to use the subject ID or a user ID claim as the user ID within the Content Reactor system, and remove the `userId` query string parameter from each API operation.

### Approach 2: Azure AD B2C

[Azure AD B2C](https://docs.microsoft.com/en-us/azure/active-directory-b2c/) is a fully managed identity system for consumer-facing apps. It uses open standards, including JSON Web Tokens (JWTs) and OpenID Connect, allowing for interoperability across a range of client and server platforms. It also provides additional features including federation with other identity providers, and advanced policies and claims.

Integrating Content Reactor with Azure AD B2C is conceptually similar to working with any other identity provider described above. However, there are a few small differences to be aware of.

 1. [Set up an Azure AD B2C tenant](https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-register-applications#create-an-azure-ad-b2c-tenant) and link it to your Azure subscription.
 2. [Register your instance of the Content Reactor web app with the Azure AD B2C tenant](https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-tutorials-spa).
 3. [Update the front-end application to obtain a token from Azure AD B2C](https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-tutorials-spa#update-single-page-app-code).
 4. Set up each microservice's front-end API Functions app to check for authorization. [This blog post provides specific instructions on using Azure AD B2C with Azure Functions.](https://blogs.msdn.microsoft.com/hmahrt/2017/03/07/azure-active-directory-b2c-and-azure-functions/)
 5. Update each microservice's front-end API Functions app to use the subject ID or a user ID claim as the user ID within the Content Reactor system, and remove the `userId` query string parameter from each API operation.


## Event Types Reference

| Event Type              | Publisher               | Published When                                                                                                                                         | Microservice Subscribers                                                                                                                    |
|-------------------------|-------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| AudioCreated            | Audio microservice      | New audio has been created (after the call to the `CompleteCreateAudio` function)                                                                      | <ul><li>Audio microservice's `UpdateAudioTranscript` function</li><li>Categories microservice’s `AddCategoryItem` function</li></ul>        |
| AudioDeleted            | Audio microservice      | Audio note has been deleted                                                                                                                            | <ul><li>Categories microservice’s `DeleteCategoryItem` function</li></ul>                                                                   |
| AudioTranscriptUpdated  | Audio microservice      | Audio note’s transcript has been modified                                                                                                              |                                                                                                                                             |
| CategoryCreated         | Categories microservice | Category has been created                                                                                                                              | <ul><li>Categories microservice’s `UpdateCategorySynonyms` function</li><li>Categories microservice’s `AddCategoryImage` function</li></ul> |
| CategoryDeleted         | Categories microservice | Category has been deleted                                                                                                                              |                                                                                                                                             |
| CategoryImageUpdated    | Categories microservice | Image for a category has been created                                                                                                                  |                                                                                                                                             |
| CategoryItemsUpdated    | Categories microservice | Any items have been added to or removed from a category (note: this event does not include details of items in category; issue a GET to get this list) |                                                                                                                                             |
| CategoryNameUpdated     | Categories microservice | The name of a category has been changed                                                                                                                |                                                                                                                                             |
| CategorySynonymsUpdated | Categories microservice | The synonyms have been added or updated for a category (i.e. after the category is added or the name is updated)                                       |                                                                                                                                             |
| ImageCaptionUpdated     | Images microservice     | The caption for an image has been updated (i.e. after the image has been created)                                                                      |                                                                                                                                             |
| ImageCreated            | Images microservice     | New image note has been created (after the call to the `CompleteCreateImage` function)                                                                 | <ul><li>Images microservice’s `UpdateImageCaption` function</li><li>Categories microservice’s `AddCategoryItem` function</li></ul>          |
| ImageDeleted            | Images microservice     | Image note has been deleted                                                                                                                            | <ul><li>Categories microservice’s `DeleteCategoryItem` function</li></ul>                                                                   |
| TextCreated             | Text microservice       | New text note has been created                                                                                                                         | <ul><li>Categories microservice’s `AddCategoryItem` function</li></ul>                                                                      |
| TextDeleted             | Text microservice       | Text note has been deleted                                                                                                                             | <ul><li>Categories microservice’s `DeleteCategoryItem` function</li></ul>                                                                   |
| TextUpdated             | Text microservice       | Whenever the contents of a text note has been changed                                                                                                  |                                                                                                                                             |

## Sample Requests

Note that these sample requests use the hostname `crprodapiproxy.azurewebsites.net`. You should replace this with your proxy app's hostname.

### Categories Microservice's API

#### Create Category

``` curl
POST https://crprodapiproxy.azurewebsites.net/api/categories?userId={userId} HTTP/1.1

{
  "name": "{name}"
}
```

Should return an HTTP 200 OK with an ‘id’.

#### Get Category

``` curl
GET https://crprodapiproxy.azurewebsites.net/api/categories/{categoryId}?userId={userId} HTTP/1.1
```

Should return an HTTP 200 OK with the category details, including (after a short period of time) the image URL and synonyms, and any items added to the category.

#### List Categories for User

``` curl
GET https://crprodapiproxy.azurewebsites.net/api/categories?userId={userId} HTTP/1.1
```

Should return an HTTP 200 OK with the category summary – the name and ID of the category.

#### Update Category Name

``` curl
PATCH https://crprodapiproxy.azurewebsites.net/api/categories/{categoryId}?userId={userId} HTTP/1.1

{
  "name": "{newName}"
}
```

Should return an HTTP 204 No Content.

#### Delete Category

``` curl
DELETE https://crprodapiproxy.azurewebsites.net/api/categories/{categoryId}?userId={userId} HTTP/1.1
```

Should return an HTTP 204 No Content.

### Audio Microservice's API

#### Create Audio Note

Note that creating an audio note is done in three parts. First, the client should issue a request to the `audio` API without a payload:

``` curl
POST https://crprodapiproxy.azurewebsites.net/audio?userId={userId} HTTP/1.1
```

This will return an HTTP 200 OK with a response body that includes an `id` (the audio note's ID) and an `url` (an Azure Storage blob URL with a [SAS token](https://docs.microsoft.com/en-us/azure/storage/common/storage-dotnet-shared-access-signature-part-1) allowing the client to upload the audio file to this location).

Second, the client should use the `url` parameter they obtained to upload the audio file as a blob using the Azure Storage SDK or a REST API.

Third, the client should make the following request to the `audio` API:

``` curl
POST https://crprodapiproxy.azurewebsites.net/audio/{audioId}?userId={userId} HTTP/1.1

{
  "categoryId": "{categoryId}"
}
```

The `id` parameter they obtained should be included in the URL, and they should also include the ID for the category that this note should be placed into. Should return an HTTP 204 No Content.

#### Get Audio Note

``` curl
GET https://crprodapiproxy.azurewebsites.net/audio/{audioId}?userId={userId} HTTP/1.1
```

Should return an HTTP 200 OK with the audio note details, including the audio file's URL, and (after a short time) the transcript. The URL includes a time-limited SAS token allowing the client to read the blob.

#### List Audio Notes for User

``` curl
GET https://crprodapiproxy.azurewebsites.net/audio?userId={userId} HTTP/1.1
```

Should return an HTTP 200 OK with the audio notes summary – the ID of each audio note and preview of its transcript, if known.

#### Delete Audio Note

``` curl
DELETE https://crprodapiproxy.azurewebsites.net/audio/{audioId}?userId={userId} HTTP/1.1
```

Should return an HTTP 204 No Content.

### Images Microservice's API

#### Create Image Note

Note that creating an image note is done in three parts. First, the client should issue a request to the `images` API without a payload:

``` curl
POST https://crprodapiproxy.azurewebsites.net/images?userId={userId} HTTP/1.1
```

This will return an HTTP 200 OK with a response body that includes an `id` (the image note's ID) and a `url` (an Azure Storage blob URL with a [SAS token](https://docs.microsoft.com/en-us/azure/storage/common/storage-dotnet-shared-access-signature-part-1) allowing the client to upload the image to this location).

Second, the client should use the `url` parameter they obtained to upload the image file as a blob using the Azure Storage SDK or a REST API.

Third, the client should make the following request to the `images` API:

``` curl
POST https://crprodapiproxy.azurewebsites.net/images/{imageId}?userId={userId} HTTP/1.1

{
  "categoryId": "{categoryId}"
}
```

The `id` parameter they obtained should be included in the URL, and they should also include the ID for the category that this note should be placed into. Should return an HTTP 200 OK with the preview URL included.

#### Get Image Note

``` curl
GET https://crprodapiproxy.azurewebsites.net/images/{imageId}?userId={userId} HTTP/1.1
```

Should return an HTTP 200 OK with the image note details, including the full-size image URL and preview URL, and (after a short time) the image caption. The URLs include a time-limited SAS token allowing the client to read the blob.

#### List Image Notes for User

``` curl
GET https://crprodapiproxy.azurewebsites.net/images?userId={userId} HTTP/1.1
```

Should return an HTTP 200 OK with the image notes summary – the ID of each image note and preview of the caption, if known.

#### Delete Image Note

``` curl
DELETE https://crprodapiproxy.azurewebsites.net/images/{imageId}?userId={userId} HTTP/1.1
```

Should return an HTTP 204 No Content.

### Text Microservice's API

#### Create Text Note

``` curl
POST https://crprodapiproxy.azurewebsites.net/text?userId={userId} HTTP/1.1 

{
  "text": "{text}",
  "categoryId": "{categoryId}"
}
```

Should return an HTTP 200 OK with the text note's ID.

#### Get Text Note

``` curl
GET https://crprodapiproxy.azurewebsites.net/text/{textId}?userId={userId} HTTP/1.1
```

Should return an HTTP 200 OK with the text note details, including the full text content.

#### List Text Notes for User

``` curl
GET https://crprodapiproxy.azurewebsites.net/text?userId={userId} HTTP/1.1
```

Should return an HTTP 200 OK with the text notes summary – the ID of each text note and the first 100 characters.

#### Update Text Note

Note that the text microservice is the only one that provides an update API endpoint.

``` curl
PATCH https://crprodapiproxy.azurewebsites.net/text/{textId}?userId={userId} HTTP/1.1

{

  "text": "{newText}"

}
```

Should return an HTTP 204 No Content.

#### Delete Text Note

``` curl
DELETE https://crprodapiproxy.azurewebsites.net/text/{textId}?userId={userId} HTTP/1.1
```

Should return an HTTP 204 No Content.

## UI Screenshots

Here are some sample screenshots from the front end that covers all scenarios and most screens.

### Login Screen

![Screen 1](/_docs/screens/screen1.png)

### Category CRUD screens

#### Category Create

![Screen 3](/_docs/screens/screen3.png)

#### Category Image/Synonym Update Event

![Screen 4](/_docs/screens/screen4.png)

#### Category Name Update and Image change notification

![Screen 5](/_docs/screens/screen5.png)

### Image CRUD and notification screens

#### Image Create

![Screen 6](/_docs/screens/screen6.png)

#### Image Caption Updated through Event Grid notification 

![Screen 7](/_docs/screens/screen7.png)

![Screen 11](/_docs/screens/screen11.png)

### Text CRUD and notification screens

#### Text Note create

![Screen 8](/_docs/screens/screen8.png)

### Text Note show

![Screen 10](/_docs/screens/screen10.png)

### Audio CRUD and notification screens

#### Audio create

![Screen 12](/_docs/screens/screen12.png)

#### Audio processing

![Screen 13](/_docs/screens/screen13.png)

#### Audio transcript event grid notification

![Screen 14](/_docs/screens/screen14.png)

#### Audio show

![Screen 15](/_docs/screens/screen15.png)

### Events at category level

Events are displayed as notifications against a category.

![Screen 16](/_docs/screens/screen16.png)