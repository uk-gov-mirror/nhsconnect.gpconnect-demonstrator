'use strict';

angular.module('gpConnect')
  .controller('MedicationsListCtrl', function ($scope, $location, $stateParams, $modal, $state, $sce, usSpinnerService, PatientService, Medication) {

    $scope.query = {};
    $scope.queryBy = '$';

    $scope.currentPage = 1;

    $scope.pageChangeHandler = function (newPage) {
      $scope.currentPage = newPage;
    };

    if ($stateParams.page) {
      $scope.currentPage = $stateParams.page;
    }

    PatientService.findDetails($stateParams.patientId).then(function (patient) {
      $scope.patient = patient.data;
    });

    if ($stateParams.filter) {
      $scope.query.$ = $stateParams.filter;
    }

    Medication.findAllHTMLTables($stateParams.patientId).then(function (result) {
      $scope.medicationTables = result.data;

      for (var i = 0; i < $scope.medicationTables.length; i++) {
        $scope.medicationTables[i].html = $sce.trustAsHtml($scope.medicationTables[i].html);
      }

      usSpinnerService.stop('medicationSummary-spinner');
    });

    $scope.go = function (id, source) {
      $state.go('medications-detail', {
        patientId: $scope.patient.id,
        medicationIndex: id,
        filter: $scope.query.$,
        page: $scope.currentPage,
        source: source
      });
    };

    $scope.selected = function (medicationIndex) {
      return medicationIndex === $stateParams.medicationIndex;
    };

    $scope.create = function () {
      var modalInstance = $modal.open({
        templateUrl: 'views/medications/medications-modal.html',
        size: 'lg',
        controller: 'MedicationsModalCtrl',
        resolve: {
          modal: function () {
            return {
              title: 'Create Medication'
            };
          },
          medication: function () {
            return {};
          },
          patient: function () {
            return $scope.patient;
          }
        }
      });

      modalInstance.result.then(function (medication) {
        medication.startDate = new Date(medication.startDate);
        medication.startTime = new Date(medication.startTime.valueOf() - medication.startTime.getTimezoneOffset() * 60000);

        var toAdd = {
          doseAmount: medication.doseAmount,
          doseDirections: medication.doseDirections,
          doseTiming: medication.doseTiming,
          medicationCode: medication.medicationCode,
          medicationTerminology: medication.medicationTerminology,
          name: medication.name,
          route: medication.route,
          startDate: medication.startDate,
          startTime: medication.startTime,
          author: medication.author,
          dateCreated: medication.dateCreated
        };

        Medication.create($scope.patient.id, toAdd).then(function () {
          setTimeout(function () {
            $state.go('medications', {
              patientId: $scope.patient.id,
              filter: $scope.query.$,
              page: $scope.currentPage
            }, {
              reload: true
            });
          }, 2000);
        });
      });
    };

  });
