﻿(function (app) {
    app.controller('warningprofileCtrl', function deviceCtrl($scope, DTOptionsBuilder, apiService, $ngBootbox, DTColumnDefBuilder, DTColumnBuilder, notificationService) {
        $scope.dtOptions = DTOptionsBuilder.newOptions()
            .withDOM('<"html5buttons"B>lTfgitp')
            .withButtons([
                { extend: 'copy' },
                { extend: 'excel' },
                { extend: 'pdf' },
                {
                    extend: 'print',
                    customize: function (win) {
                        $(win.document.body).addClass('white-bg');
                        $(win.document.body).css('font-size', '10px');
                        $(win.document.body).find('table')
                            .addClass('compact')
                            .css('font-size', 'inherit');
                    }
                }
            ]);
        $scope.dtColumnDefs = [
            DTColumnDefBuilder.newColumnDef(0).notSortable(),
            DTColumnDefBuilder.newColumnDef(1),
            DTColumnDefBuilder.newColumnDef(2),
            DTColumnDefBuilder.newColumnDef(3),
            DTColumnDefBuilder.newColumnDef(4),
            DTColumnDefBuilder.newColumnDef(5),
            DTColumnDefBuilder.newColumnDef(6),

        ];
        $('select').chosen({ width: '100%' });
        $scope.warningprofiles = [];
        $scope.warningNames = warningNames;
        $scope.deleteWarningProfile = deleteWarningProfile;
        $scope.initData = () => {
            $scope.getAll();
        }
        function deleteWarningProfile(Id) {
            $ngBootbox.confirm('Bạn có chắc muốn xóa?')
                .then(function () {
                    var config = {
                        params: {
                            id: Id
                        }
                    }
                    apiService.del('/api/warningprofile/delete', config, function () {
                        notificationService.displaySuccess('Đã xóa thành công.');

                    },
                        function () {
                            notificationService.displayError('Xóa không thành công.');
                        });
                });
        }


        $scope.getAll = () => {
            apiService.get('/api/warningprofile/getwarningprofile', null, function (result) {
                $scope.warningprofiles = result.data
            }, function () {
                console.log('Load warningprofiles failed.');
            });
        }

        $scope.resetWarningProfile = () => {
            apiService.post('/api/warningprofile/reset', null, function (result) {
                notificationService.displaySuccess(result.data);
                $scope.getAll()
            }, function (error) {
                console.log(error.data);
            });
        }
        //Add/Update Device
        $scope.EditWarningProfile = true;
        $scope.warningprofile = {
        };
        $scope.enableAddWarningProfile = function () {
            $scope.isEditWarningProfile = false;
            $('#form-warning').modal('show');
            $scope.warningprofile = {
                isActive: true,
            };
        }
        $scope.enableEditWarningProfile = function (warningprofile) {
            $scope.isEditWarningProfile = true;
            $('#form-warning').modal('show');
            $scope.warningprofile = angular.copy(warningprofile);
        }
        $scope.updateWarningProfile = () => {
            apiService.post('/api/warningprofile/update', $scope.warningprofile,
                function (result) {
                    warningprofile = result.data;
                    if ($scope.isEditWarningProfile) {
                        for (var i = 0; i < $scope.warningprofiles.length; i++) {
                            if ($scope.warningprofiles[i].Id == warningprofile.Id) {
                                $scope.warningprofiles[i] = warningprofile;
                                notificationService.displaySuccess('Đã cập nhật cấu hình ');
                                break;
                            }
                        }
                    } else {
                        $scope.warningprofiles.push(warningprofile);
                    }
                    $scope.closeEditForm();
                }, function (error) {
                    console.log(error)
                    notificationService.displayError(error.data.Message);
                    $scope.closeEditForm();
                });
        }
        $scope.closeEditForm = () => {
            $('#form-warning').modal('hide');
        }

    });
})(angular.module('app'));