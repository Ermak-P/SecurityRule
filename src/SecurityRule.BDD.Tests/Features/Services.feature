Feature: AppService Management
  As a system administrator
  I want to manage application services
  So that I can track which services run on which servers

  Scenario: Add a new service
    Given the service database is empty
    And a server "Host-Server" with IP "10.0.0.1" and OS "Linux" exists
    When I add a service with name "PaymentService" and AD account "domain\\payment"
    Then the service list should contain 1 service
    And the service "PaymentService" should exist in the list

  Scenario: View all services
    Given a server "Host-Server" with IP "10.0.0.1" and OS "Linux" exists
    And the following services exist:
      | Name           | AdAccountName    |
      | AuthService    | domain\\auth     |
      | ReportService  | domain\\report   |
    When I request all services
    Then the service list should contain 2 services

  Scenario: Find a service by ID
    Given a server "Host-Server" with IP "10.0.0.1" and OS "Linux" exists
    And a service "SearchService" with AD account "domain\\search" exists
    When I search for the service by its ID
    Then the service should be found
    And the service name should be "SearchService"

  Scenario: Service not found by ID
    Given the service database is empty
    When I search for the service with ID 999
    Then no service should be found

  Scenario: Update a service
    Given a server "Host-Server" with IP "10.0.0.1" and OS "Linux" exists
    And a service "OldService" with AD account "domain\\old" exists
    When I update the service name to "NewService"
    Then the service should have the name "NewService"

  Scenario: Delete a service
    Given a server "Host-Server" with IP "10.0.0.1" and OS "Linux" exists
    And a service "TempService" with AD account "domain\\temp" exists
    When I delete the service
    Then the service list should be empty

  Scenario: Service linked to multiple servers
    Given the following servers exist:
      | Name    | IpAddress | OperatingSystem |
      | Server1 | 10.0.0.1  | Linux           |
      | Server2 | 10.0.0.2  | Windows         |
    When I add a service "SharedService" with AD account "domain\\shared" linked to both servers
    Then the service list should contain 1 service
    And the service "SharedService" should be linked to 2 servers

  Scenario: Service retrieval includes linked servers
    Given a server "Host-Server" with IP "10.0.0.1" and OS "Linux" exists
    And a service "LinkedService" with AD account "domain\\linked" exists
    When I search for the service by its ID
    Then the service should include 1 server
