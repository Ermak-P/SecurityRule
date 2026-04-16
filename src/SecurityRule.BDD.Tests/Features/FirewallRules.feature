Feature: Firewall Rule Management
  As a security engineer
  I want to manage firewall rules
  So that I can control network access and track rule expiry

  Scenario: Add a new firewall rule
    Given the firewall rule database is empty
    When I add a firewall rule from "192.168.1.0" to "10.0.0.0" expiring in 1 year with description "Allow HTTP"
    Then the firewall rule list should contain 1 rule
    And the rule with source "192.168.1.0" should exist in the list

  Scenario: View all firewall rules
    Given the following firewall rules exist:
      | SourceIp    | DestinationIp | ExpiresInDays | Description     |
      | 10.0.0.1    | 10.0.0.2      | 365           | Allow HTTPS     |
      | 172.16.0.1  | 172.16.0.2    | 180           | Allow SSH       |
    When I request all firewall rules
    Then the firewall rule list should contain 2 rules

  Scenario: Find a firewall rule by ID
    Given a firewall rule from "10.1.1.1" to "10.2.2.2" expiring in 1 year with description "Test rule" exists
    When I search for the firewall rule by its ID
    Then the firewall rule should be found
    And the firewall rule source IP should be "10.1.1.1"

  Scenario: Firewall rule not found by ID
    Given the firewall rule database is empty
    When I search for the firewall rule with ID 999
    Then no firewall rule should be found

  Scenario: Update a firewall rule
    Given a firewall rule from "10.0.0.1" to "10.0.0.2" expiring in 1 year with description "Old description" exists
    When I update the firewall rule description to "New description"
    Then the firewall rule should have the description "New description"

  Scenario: Delete a firewall rule
    Given a firewall rule from "192.168.1.1" to "192.168.1.2" expiring in 1 year with description "Temp rule" exists
    When I delete the firewall rule
    Then the firewall rule list should be empty

  Scenario: Identify an expired firewall rule
    Given the firewall rule database is empty
    When I add a firewall rule from "10.0.0.1" to "10.0.0.2" that expired 1 day ago with description "Expired rule"
    Then the firewall rule "Expired rule" should have an expiry date in the past

  Scenario: Identify a firewall rule expiring soon
    Given the firewall rule database is empty
    When I add a firewall rule from "10.0.0.1" to "10.0.0.2" expiring in 15 days with description "Expiring soon"
    Then the firewall rule "Expiring soon" should expire within 30 days

  Scenario: Identify an active firewall rule
    Given the firewall rule database is empty
    When I add a firewall rule from "10.0.0.1" to "10.0.0.2" expiring in 90 days with description "Active rule"
    Then the firewall rule "Active rule" expiry date should be more than 30 days from now

  Scenario: Delete a non-existent firewall rule does not throw
    Given the firewall rule database is empty
    When I delete the firewall rule with ID 999
    Then no exception should be thrown for the firewall deletion
