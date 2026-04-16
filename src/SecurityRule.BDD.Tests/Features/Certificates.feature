Feature: Certificate Management
  As a system administrator
  I want to manage SSL/TLS certificates
  So that I can track certificate expiry and associated services

  Scenario: Add a new certificate
    Given the certificate database is empty
    When I add a certificate with description "TLS cert for api.example.com", issued 1 year ago and expiring in 1 year
    Then the certificate list should contain 1 certificate
    And the certificate "TLS cert for api.example.com" should exist in the list

  Scenario: View all certificates
    Given the following certificates exist:
      | Description   | IssuedDaysAgo | ExpiresInDays |
      | Cert-Alpha    | 365           | 365           |
      | Cert-Beta     | 180           | 540           |
    When I request all certificates
    Then the certificate list should contain 2 certificates

  Scenario: Find a certificate by ID
    Given a certificate "My Cert" issued 1 year ago and expiring in 2 years exists
    When I search for the certificate by its ID
    Then the certificate should be found
    And the certificate description should be "My Cert"

  Scenario: Certificate not found by ID
    Given the certificate database is empty
    When I search for the certificate with ID 999
    Then no certificate should be found

  Scenario: Update a certificate
    Given a certificate "Old Cert" issued today and expiring in 1 year exists
    When I update the certificate description to "Updated Cert"
    Then the certificate should have the description "Updated Cert"

  Scenario: Delete a certificate
    Given a certificate "Temp Cert" issued today and expiring in 1 year exists
    When I delete the certificate
    Then the certificate list should be empty

  Scenario: Identify an expired certificate
    Given the certificate database is empty
    When I add a certificate with description "Expired cert", issued 2 years ago and expiry 1 day ago
    Then the certificate "Expired cert" should have an expiry date in the past

  Scenario: Identify a certificate expiring soon
    Given the certificate database is empty
    When I add a certificate with description "Expiring soon", issued 1 year ago and expiring in 15 days
    Then the certificate "Expiring soon" should expire within 30 days

  Scenario: Identify an active certificate
    Given the certificate database is empty
    When I add a certificate with description "Active cert", issued 1 year ago and expiring in 180 days
    Then the certificate "Active cert" expiry date should be more than 30 days from now
