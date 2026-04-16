Feature: Server Management
  As a system administrator
  I want to manage servers
  So that I can track my infrastructure

  Scenario: Add a new server
    Given the server database is empty
    When I add a server with name "Web-Server-01", IP "192.168.1.1" and OS "Linux"
    Then the server list should contain 1 server
    And the server "Web-Server-01" should exist in the list

  Scenario: View all servers
    Given the following servers exist:
      | Name         | IpAddress | OperatingSystem |
      | Server-Alpha | 10.0.0.1  | Linux           |
      | Server-Beta  | 10.0.0.2  | Windows         |
    When I request all servers
    Then the server list should contain 2 servers

  Scenario: Find a server by ID
    Given a server "DB-Server" with IP "172.16.0.1" and OS "Ubuntu" exists
    When I search for the server by its ID
    Then the server should be found
    And the server name should be "DB-Server"

  Scenario: Server not found by ID
    Given the server database is empty
    When I search for the server with ID 999
    Then no server should be found

  Scenario: Update a server
    Given a server "Old-Name" with IP "10.1.1.1" and OS "CentOS" exists
    When I update the server name to "New-Name"
    Then the server should have the name "New-Name"

  Scenario: Delete a server
    Given a server "Temp-Server" with IP "192.168.0.99" and OS "Debian" exists
    When I delete the server
    Then the server list should be empty

  Scenario: Delete a non-existent server does not throw
    Given the server database is empty
    When I delete the server with ID 999
    Then no exception should be thrown

  Scenario: Server includes its associated services
    Given a server "App-Server" with IP "10.0.1.1" and OS "Linux" exists
    And a service "MyService" with AD account "domain\\svc" is linked to the server
    When I search for the server by its ID
    Then the server should include 1 service
