package uk.gov.hscic.common.filters.model;

import ca.uhn.fhir.rest.server.exceptions.InvalidRequestException;
import ca.uhn.fhir.rest.server.exceptions.UnprocessableEntityException;
import java.util.Arrays;
import java.util.List;

import org.hl7.fhir.dstu3.model.OperationOutcome.IssueType;

import uk.gov.hscic.OperationOutcomeFactory;
import uk.gov.hscic.SystemCode;

public class WebTokenValidator {
    private static final List<String> PERMITTED_REQUESTED_SCOPES = Arrays.asList("patient/*.read", "patient/*.write",
            "organization/*.read", "organization/*.write");

    public static void validateWebToken(WebToken webToken, int futureRequestLeeway) {
        verifyNoNullValues(webToken);
        verifyTimeValues(webToken, futureRequestLeeway);
        verifyRequestedResourceValues(webToken);

        // Checking the practionerId and the sub are equal in value
        if (!(webToken.getRequestingPractitioner().getId().equals(webToken.getSub()))) {
            throw OperationOutcomeFactory.buildOperationOutcomeException(
                    new InvalidRequestException("JWT Practitioner ids do not match!"), SystemCode.BAD_REQUEST,
                    IssueType.INVALID);
        }

        if (!PERMITTED_REQUESTED_SCOPES.contains(webToken.getRequestedScope())) {
            throw OperationOutcomeFactory.buildOperationOutcomeException(
                    new InvalidRequestException("JWT Bad Request Exception Invalid requested scope: "+webToken.getRequestedScope()), SystemCode.BAD_REQUEST,
                    IssueType.INVALID);
        }
    }

    private static void verifyNoNullValues(WebToken webToken) {
        assertNotNull("aud",webToken.getAud());
        assertNotNull("exp",webToken.getExp());
        assertNotNull("iat",webToken.getIat());
        assertNotNull("iss",webToken.getIss());
        assertNotNull("sub",webToken.getSub());
        assertNotNull("reason_for_request",webToken.getReasonForRequest());
        assertNotNull("requested_scope",webToken.getRequestedScope());
        assertNotNull("requesting_device",webToken.getRequestingDevice());
        assertNotNull("requesting_device.resourceType",webToken.getRequestingDevice().getResourceType());
        assertNotNull("requesting_organization",webToken.getRequestingOrganization());
        assertNotNull("requesting_organization.resourceType",webToken.getRequestingOrganization().getResourceType());
        assertNotNull("requesting_practitioner",webToken.getRequestingPractitioner());
        assertNotNull("requesting_practitioner.resourceType",webToken.getRequestingPractitioner().getResourceType());

    }

    private static void assertNotNull(String claim, Object object) {
        if (null == object) {
            throw OperationOutcomeFactory.buildOperationOutcomeException(
                    new InvalidRequestException("JWT JSON entry incomplete: claim " + claim + " is null."), SystemCode.BAD_REQUEST,
                    IssueType.INVALID);
        }
    }

    private static void verifyTimeValues(WebToken webToken, int futureRequestLeeway) {
        // Checking the creation date is not in the future
        int timeValidationIdentifierInt = webToken.getIat();

        // Checking creation time is not in the future (with a 5 second leeway
        if (timeValidationIdentifierInt > (System.currentTimeMillis() / 1000) + futureRequestLeeway) {
            throw OperationOutcomeFactory.buildOperationOutcomeException(
                    new InvalidRequestException("JWT Creation time is in the future"), SystemCode.BAD_REQUEST,
                    IssueType.INVALID);
        }

        // Checking the expiry time is 5 minutes after creation
        if (webToken.getExp() - timeValidationIdentifierInt != 300) {
            throw OperationOutcomeFactory.buildOperationOutcomeException(
                    new InvalidRequestException("JWT Request time expired"), SystemCode.BAD_REQUEST,
                    IssueType.INVALID);
        }
    }

    private static void verifyRequestedResourceValues(WebToken webToken) {
        // Checking the reason for request is directcare
        if (!"directcare".equals(webToken.getReasonForRequest())) {
            throw OperationOutcomeFactory.buildOperationOutcomeException(
                    new InvalidRequestException("JWT Reason for request is not directcare"), SystemCode.BAD_REQUEST,
                    IssueType.INVALID);
        }

        RequestingDevice requestingDevice = webToken.getRequestingDevice();

        if (null == requestingDevice) {
            throw OperationOutcomeFactory.buildOperationOutcomeException(
                    new InvalidRequestException("JWT No requesting_device"), SystemCode.BAD_REQUEST,
                    IssueType.INVALID);
        }

        String deviceType = requestingDevice.getResourceType();
        String organizationType = webToken.getRequestingOrganization().getResourceType();
        String practitionerType = webToken.getRequestingPractitioner().getResourceType();
        
        if (!deviceType.equals("Device") || !organizationType.equals("Organization") || !practitionerType.equals("Practitioner")) {
            throw OperationOutcomeFactory.buildOperationOutcomeException(
                    new UnprocessableEntityException("JWT Invalid resource type"), 
                    SystemCode.BAD_REQUEST,
                    IssueType.INVALID
            );
        }
    }   
}
