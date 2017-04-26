## Components
#### Event Feed and Domain
Features: 
- Raise events : the code in a service domain model raises events when something significant (account to the business rules) happens.
    Significant events are when items are added to or removed from a shopping cart.
- Store events : The events raised by the Service domain model are stored in the microservice's data store 
- Publish events : Implementing an event feed allows other microservices to sub-scribe by polling


## Vocab
Nancy Module: Nancy module is a class that inherits from NancyModule and is used to implement endpoints in a Nancy application.

