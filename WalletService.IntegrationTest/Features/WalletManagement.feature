Feature: Wallet Management
As a developer, I want to ensure that the wallet creation, updating, and retrieval processes work as expected.
I also want to ensure that the system handles concurrency issues correctly.

    Background:
        Given a running Wallet API

    Scenario: Successful Wallet Creation
        Given I have a valid wallet creation request
        When I send the create wallet request
        Then the wallet should be created successfully

    Scenario: Update Existing Wallet
        Given I have an existing wallet
        And I have a valid wallet update request
        When I send the update wallet request
        Then the wallet should be updated successfully

    Scenario: Retrieve Existing Wallet
        Given I have an existing wallet
        When I send a get wallet request
        Then the wallet details should be retrieved successfully